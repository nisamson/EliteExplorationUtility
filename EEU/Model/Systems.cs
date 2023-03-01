using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using EEU.Utils;
using EEU.Utils.Init;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Nullable.Extensions;
using Assert = EEU.Utils.Assert;

namespace EEU.Model;

public class TypeEntityMapping {
    private IList<(Type, List<IUpsertHandler>)> Inner { get; } = new List<(Type, List<IUpsertHandler>)>();

    public IEnumerable<(Type, ReadOnlyCollection<IUpsertHandler>)> Entities {
        get { return Inner.Select(tuple => (tuple.Item1, tuple.Item2.AsReadOnly())); }
    }

    private List<IUpsertHandler> GetTypeList<T>() where T : class, IUpsertHandler {
        var tType = typeof(T);
        return GetTypeList(tType);
    }

    private List<IUpsertHandler> GetTypeList(Type tType) {
        Debug.Assert(tType.IsAssignableTo(typeof(IUpsertHandler)));

        List<IUpsertHandler>? listOrNot = null;
        foreach (var (t, l) in Inner) {
            if (t == tType) {
                listOrNot = l;
            }
        }

        if (listOrNot is null) {
            listOrNot = new List<IUpsertHandler>();
            Inner.Add((tType, listOrNot));
        }

        return listOrNot;
    }

    public void AddEntity<T>(T entity) where T : class, IUpsertHandler {
        GetTypeList<T>().Add(entity);
        entity.Let(x => x.GatherChildEntities(this));
    }

    public void AddAllEntities<T>(IEnumerable<T> elems) where T : class, IUpsertHandler {
        var target = GetTypeList<T>();
        target.AddRange(
            elems.Select(
                x => {
                    x.GatherChildEntities(this);
                    return x;
                }
            )
        );
    }

    private TypeEntityMapping Merge(TypeEntityMapping other) {
        foreach (var (t, l) in other.Inner) {
            GetTypeList(t).AddRange(l);
        }

        return this;
    }
}

public interface IUpsertHandler {
    public void HandleUpsertChildren(EEUContext ctx, BulkConfig config);

    public Task HandleUpsertChildrenAsync(EEUContext ctx, BulkConfig config);

    public void PrepareForUpsert();

    public void GatherChildEntities(TypeEntityMapping tem);

    public TypeEntityMapping GatherChildEntities() {
        var start = new TypeEntityMapping();
        GatherChildEntities(start);
        return start;
    }
}

public static class UpsertHelper {
    private static readonly int MaxInsertBufferColumnCount = 999;

    public static void Upsert<T>(this T handler, EEUContext ctx, BulkConfig cfg) where T : class, IUpsertHandler {
        ctx.BulkInsertOrUpdate(new[] { handler }, cfg);
        handler.HandleUpsertChildren(ctx, cfg);
    }

    public static async Task UpsertAsync<T>(this T handler, EEUContext ctx, BulkConfig cfg) where T : class, IUpsertHandler {
        await new[] { handler }.UpsertAllAsync(ctx, cfg);
    }

    public static void UpsertAll<T>(this IList<T> elems, EEUContext ctx, BulkConfig cfg) where T : class, IUpsertHandler {
        ctx.BulkInsertOrUpdate(elems.ToArray(), cfg);

        foreach (var elem in elems) {
            elem.HandleUpsertChildren(ctx, cfg);
        }
    }

    public static async Task UpsertAllAsync<T>(this IList<T> elems, EEUContext ctx, BulkConfig cfg) where T : class, IUpsertHandler {
        await ctx.BulkInsertOrUpdateAsync(elems, cfg);
        foreach (var elem in elems) {
            await elem.HandleUpsertChildrenAsync(ctx, cfg);
        }
    }

    public static void PrepareAllForUpsert<T>(this IList<T> elems) where T : class, IUpsertHandler {
        foreach (var upsertHandler in elems) {
            upsertHandler.PrepareForUpsert();
        }
    }

    public static TypeEntityMapping GatherEntities<T>(this IEnumerable<T> elems) where T : class, IUpsertHandler {
        TypeEntityMapping o = new();
        o.AddAllEntities(elems);
        return o;
    }
}

[Serializable]
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
[Index(nameof(Name))]
public class System : IUpsertHandler {
    public System(ulong id64, string name, DateTime updated) {
        Id64 = id64;
        Name = name;
        Updated = updated;
    }

    public bool IsEligible() {
        return Bodies.Any(b => b.IsEligibleBody()) || Population > 0;
    }

    [JsonProperty(Required = Required.Always)]
    public ulong Id64 { get; set; }

    [JsonProperty(Required = Required.Always)]
    [Column(TypeName = "nvarchar(250)")]
    public string Name { get; set; }

    [JsonProperty(Required = Required.Always)]
    public Coords Coords { get; set; } = new();

    public ulong Population { get; set; }

    public ulong BodyCount { get; set; }

    [JsonProperty(PropertyName = "date", Required = Required.Always)]
    [JsonConverter(typeof(IsoDateTimeConverter))]
    public DateTime Updated { get; set; }

    [JsonProperty(Required = Required.Always)]
    public List<Body> Bodies { get; set; } = new();

    public void HandleUpsertChildren(EEUContext ctx, BulkConfig cfg) {
        Bodies.Where(b => b.ShouldPersist()).ToList().UpsertAll(ctx, cfg);
    }

    public async Task HandleUpsertChildrenAsync(EEUContext ctx, BulkConfig config) {
        await Bodies.UpsertAllAsync(ctx, config);
    }

    public void PrepareForUpsert() {
        Bodies.ForEach(
            b => {
                b.SystemId64 = Id64;
                b.System = this;
            }
        );
        Bodies.PrepareAllForUpsert();
    }

    public void GatherChildEntities(TypeEntityMapping tem) {
        tem.AddAllEntities(Bodies.Where(b => b.IsEligibleBody()));
    }
}

[Owned]
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy), ItemRequired = Required.Always)]
public class Coords {
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

[Serializable]
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
[Index(nameof(Name))]
[Index(nameof(SystemId64), nameof(BodyId), IsUnique = true)]
public class Body : IUpsertHandler {
    public Body(ulong id64, ulong bodyId, string type, string name, DateTime lastUpdated) {
        Id64 = id64;
        BodyId = bodyId;
        Type = type;
        Name = name;
        LastUpdated = lastUpdated;
    }

    public bool IsEligibleBody() {
        return Type == "Planet" && IsLandable;
    }

    public bool ShouldPersist() {
        return Type is "Planet" or "Star";
    }

    [JsonIgnore] public ulong SystemId64 { get; set; }

    [ForeignKey("SystemId64")]
    [JsonIgnore]
    public System System { get; set; } = null!;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonProperty(Required = Required.Always)]
    public ulong Id64 { get; set; }

    [JsonProperty(Required = Required.Always)]
    public ulong BodyId { get; set; }

    [Column(TypeName = "nvarchar(100)")] public string? Type { get; set; }

    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; }

    public double? SemiMajorAxis { get; set; }
    public double? OrbitalEccentricity { get; set; }
    public double? OrbitalInclination { get; set; }
    public double? AgeOfPeriapsis { get; set; }
    public double? MeanAnomaly { get; set; }
    public double? AscendingNode { get; set; }

    // [AllowNull]
    // [JsonProperty(Required = Required.Always)]
    // public SortedSet<Station> Stations { get; set; }

    [JsonProperty(Required = Required.Always, PropertyName = "updateTime")]
    [JsonConverter(typeof(IsoDateTimeConverter))]
    public DateTime LastUpdated { get; set; }

    [Column(TypeName = "nvarchar(100)")] public string? SubType { get; set; }

    public double? DistanceToArrival { get; set; }

    public bool? MainStar { get; set; }

    public long? Age { get; set; }

    public string? SpectralClass { get; set; }

    public string? Luminosity { get; set; }

    public double? AbsoluteMagnitude { get; set; }

    public double? SolarMasses { get; set; }

    public double? SolarRadius { get; set; }

    public double? SurfaceTemperature { get; set; }

    public double? RotationalPeriod { get; set; }

    public bool? RotationalPeriodTidallyLocked { get; set; }

    public double? AxialTilt { get; set; }

    [InverseProperty(nameof(Body))] public List<Parent> Parents { get; set; } = new();

    [DefaultValue(false)] public bool IsLandable { get; set; }

    public double? Gravity { get; set; }

    public double? EarthMasses { get; set; }

    public double? Radius { get; set; }

    public double? SurfacePressure { get; set; }

    public string? AtmosphereType { get; set; }

    [InverseProperty(nameof(Body))] public AtmosphereComposition? AtmosphereComposition { get; set; }

    public string? TerraformingState { get; set; }

    public string? VolcanismType { get; set; }

    [InverseProperty(nameof(Body))] public SolidComposition? SolidComposition { get; set; }

    [InverseProperty(nameof(Body))] public Materials? Materials { get; set; }

    [InverseProperty(nameof(Body))] public Signals? Signals { get; set; }

    public string? ReserveLevel { get; set; }

    public void HandleUpsertChildren(EEUContext ctx, BulkConfig cfg) {
        // Stations.UpsertAll(ctx);
        Parents.Where(x => x.IsEligible()).ToList().UpsertAll(ctx, cfg);
        Signals.Let(x => x.Upsert(ctx, cfg));
        AtmosphereComposition.Let(x => x.Upsert(ctx, cfg));
        SolidComposition.Let(x => x.Upsert(ctx, cfg));
        Materials.Let(x => x.Upsert(ctx, cfg));
    }

    public async Task HandleUpsertChildrenAsync(EEUContext ctx, BulkConfig config) {
        await Parents.UpsertAllAsync(ctx, config);
        await Signals.LetAsync(x => x.UpsertAsync(ctx, config));
    }

    public void PrepareForUpsert() {
        Parents.ForEach(
            p => {
                p.BodyId64 = Id64;
                p.Body = this;
            }
        );
        Signals.Let(
            s => {
                s.BodyId64 = Id64;
                s.Body = this;
            }
        );
        Signals?.PrepareForUpsert();
        Parents.PrepareAllForUpsert();
        AtmosphereComposition.Let(
            a => {
                a.BodyId64 = Id64;
                a.Body = this;
            }
        );
        SolidComposition.Let(
            a => {
                a.BodyId64 = Id64;
                a.Body = this;
            }
        );
        Materials.Let(
            a => {
                a.BodyId64 = Id64;
                a.Body = this;
            }
        );
    }

    public void GatherChildEntities(TypeEntityMapping tem) {
        tem.AddAllEntities(Parents.Where(x => x.IsEligible()));
        Signals.Let(tem.AddEntity);
        AtmosphereComposition.Let(tem.AddEntity);
        SolidComposition.Let(tem.AddEntity);
        Materials.Let(tem.AddEntity);
    }
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class Signals : IUpsertHandler {
    [JsonConstructor]
    public Signals(DateTime lastUpdated) {
        LastUpdated = lastUpdated;
    }

    [Key]
    [ForeignKey("Id64")]
    [JsonIgnore]
    public ulong BodyId64 { get; set; }

    [JsonIgnore] public Body Body { get; set; } = null!;

    [JsonProperty(PropertyName = "signals", Required = Required.Always)]
    public Detections Detections { get; set; } = new();

    [JsonProperty(PropertyName = "updateTime", Required = Required.Always)]
    [JsonConverter(typeof(IsoDateTimeConverter))]
    public DateTime LastUpdated { get; set; }

    [InverseProperty(nameof(Signals))] public SortedSet<Genus> Genuses { get; set; } = new();

    public void HandleUpsertChildren(EEUContext ctx, BulkConfig cfg) {
        Genuses.ToList().UpsertAll(ctx, cfg);
    }

    public async Task HandleUpsertChildrenAsync(EEUContext ctx, BulkConfig config) {
        await Genuses.ToList().UpsertAllAsync(ctx, config);
    }

    public void PrepareForUpsert() {
        foreach (var genus in Genuses) {
            genus.SignalsBodyId64 = BodyId64;
            genus.Signals = this;
        }
    }

    public void GatherChildEntities(TypeEntityMapping tem) {
        tem.AddAllEntities(Genuses);
    }
}

[Owned]
public class Detections {
    [JsonProperty(PropertyName = "$SAA_SignalType_Human;")]
    public long Human { get; set; }

    [JsonProperty(PropertyName = "$SAA_SignalType_Biological;")]
    public long Biological { get; set; }

    [JsonProperty(PropertyName = "$SAA_SignalType_Geological;")]
    public long Geological { get; set; }

    [JsonProperty(PropertyName = "$SAA_SignalType_Other;")]
    public long Other { get; set; }

    [JsonProperty(PropertyName = "$SAA_SignalType_Thargoid;")]
    public long Thargoid { get; set; }

    [JsonProperty(PropertyName = "$SAA_SignalType_Guardian;")]
    public long Guardian { get; set; }

    public long Painite { get; set; }
    public long Platinum { get; set; }
    public long Rhodplumsite { get; set; }
    public long Serendibite { get; set; }
    public long Alexandrite { get; set; }
    public long Benitoite { get; set; }
    public long Monazite { get; set; }
    public long Musgravite { get; set; }
}

[JsonConverter(typeof(Converter))]
[Index(nameof(Name), "SignalsBodyId64", IsUnique = true)]
[PrimaryKey(nameof(SignalsBodyId64), nameof(Name))]
public class Genus : IComparable<Genus>, IUpsertHandler {
    public Genus(string name) {
        Name = name;
    }

    [ForeignKey("BodyId64")] [JsonIgnore] public ulong SignalsBodyId64 { get; set; }

    [JsonIgnore] public Signals Signals { get; set; } = null!;

    [Column(TypeName = "nvarchar(250)")] public string Name { get; set; }

    private class Converter : JsonConverter<Genus> {
        public override void WriteJson(JsonWriter writer, Genus? value, JsonSerializer serializer) {
            serializer.Serialize(writer, value?.Name);
        }

        public override Genus? ReadJson(JsonReader reader,
            Type objectType,
            Genus? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) {
            var value = serializer.Deserialize<string>(reader);
            return value.Bind(s => new Genus(s));
        }
    }

    public int CompareTo(Genus? other) {
        if (ReferenceEquals(this, other)) {
            return 0;
        }

        if (ReferenceEquals(null, other)) {
            return 1;
        }

        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }

    public void HandleUpsertChildren(EEUContext ctx, BulkConfig cfg) { }

    public async Task HandleUpsertChildrenAsync(EEUContext ctx, BulkConfig config) { }

    public void PrepareForUpsert() { }

    public void GatherChildEntities(TypeEntityMapping tem) { }
}

public class Materials : IUpsertHandler {
    [Key]
    [ForeignKey("Id64")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore]
    public ulong BodyId64 { get; set; }

    [JsonIgnore] public Body Body { get; set; } = null!;

    public double Antimony { get; set; }
    public double Arsenic { get; set; }
    public double Carbon { get; set; }
    public double Iron { get; set; }
    public double Nickel { get; set; }
    public double Niobium { get; set; }
    public double Phosphorus { get; set; }
    public double Sulphur { get; set; }
    public double Tin { get; set; }
    public double Zinc { get; set; }
    public double Zirconium { get; set; }
    public double Cadmium { get; set; }
    public double Manganese { get; set; }
    public double Mercury { get; set; }
    public double Tellurium { get; set; }
    public double Vanadium { get; set; }
    public double Chromium { get; set; }
    public double Germanium { get; set; }
    public double Molybdenum { get; set; }
    public double Ruthenium { get; set; }
    public double Yttrium { get; set; }
    public double Selenium { get; set; }
    public double Technetium { get; set; }
    public double Tungsten { get; set; }
    public double Polonium { get; set; }
    public void HandleUpsertChildren(EEUContext ctx, BulkConfig cfg) { }
    public async Task HandleUpsertChildrenAsync(EEUContext ctx, BulkConfig config) { }

    public void PrepareForUpsert() { }

    public void GatherChildEntities(TypeEntityMapping tem) { }
}

[JsonObject(ItemRequired = Required.Always)]
public class SolidComposition : IUpsertHandler {
    [Key]
    [ForeignKey("Id64")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore]
    public ulong BodyId64 { get; set; }

    [JsonIgnore] public Body Body { get; set; } = null!;

    public double Ice { get; set; }
    public double Metal { get; set; }
    public double Rock { get; set; }

    public void HandleUpsertChildren(EEUContext ctx, BulkConfig cfg) { }
    public async Task HandleUpsertChildrenAsync(EEUContext ctx, BulkConfig config) { }

    public void PrepareForUpsert() { }

    public void GatherChildEntities(TypeEntityMapping tem) { }
}

[JsonObject]
public class AtmosphereComposition : IUpsertHandler {
    [Key]
    [ForeignKey("Id64")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore]
    public ulong BodyId64 { get; set; }

    [JsonIgnore] public Body Body { get; set; } = null!;

    public double Helium { get; set; }
    public double Hydrogen { get; set; }

    [JsonProperty(PropertyName = "Carbon dioxide")]
    public double CarbonDioxide { get; set; }

    public double Silicates { get; set; }

    [JsonProperty(PropertyName = "Sulphur dioxide")]
    public double SulphurDioxide { get; set; }

    public double Nitrogen { get; set; }
    public double Neon { get; set; }
    public double Iron { get; set; }
    public double Argon { get; set; }
    public double Ammonia { get; set; }
    public double Methane { get; set; }
    public double Water { get; set; }
    public double Oxygen { get; set; }
    public void HandleUpsertChildren(EEUContext ctx, BulkConfig config) { }

    public async Task HandleUpsertChildrenAsync(EEUContext ctx, BulkConfig config) { }

    public void PrepareForUpsert() { }

    public void GatherChildEntities(TypeEntityMapping tem) { }
}

[JsonConverter(typeof(Converter))]
[PrimaryKey(nameof(BodyId64), nameof(Local))]
public class Parent : IUpsertHandler {
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Type {
        Null,
        Star,
        Planet,
        Ring,
    }

    public bool IsEligible() {
        return Kind == Type.Star;
    }

    private class Converter : JsonConverter<Parent> {
        public override void WriteJson(JsonWriter writer, Parent? value, JsonSerializer serializer) {
            if (value is null) {
                serializer.Serialize(writer, null);
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(TypeSupport.ToString(value.Kind));
            writer.WriteValue(value.Local);
            writer.WriteEndObject();
        }

        public override Parent? ReadJson(JsonReader reader,
            global::System.Type objectType,
            Parent? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) {
            var data = serializer.Deserialize<IDictionary<Type, ulong>>(reader);

            if (data is null) {
                return null;
            }

            var p = existingValue ?? new Parent();

            var (kind, id) = data.FirstOrDefault();

            p.Kind = kind;
            p.Local = id;

            return p;
        }
    }

    [ForeignKey("Id64")] [JsonIgnore] public ulong BodyId64 { get; set; }

    [JsonIgnore] public Body Body { get; set; } = null!;

    public ulong Local { get; set; }

    public Type Kind { get; set; }
    public void HandleUpsertChildren(EEUContext ctx, BulkConfig cfg) { }
    public async Task HandleUpsertChildrenAsync(EEUContext ctx, BulkConfig config) { }

    public void PrepareForUpsert() { }

    public void GatherChildEntities(TypeEntityMapping tem) { }
}

public static class TypeSupport {
    public static string ToString(this Parent.Type t) {
        return t switch {
            Parent.Type.Null   => "Null",
            Parent.Type.Star   => "Star",
            Parent.Type.Planet => "Planet",
            Parent.Type.Ring   => "Ring",
            _                  => throw new ArgumentOutOfRangeException(nameof(t), t, null),
        };
    }
}

internal class PointConverter : JsonConverter<Point> {
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy), ItemRequired = Required.Always)]
    internal class RawCoords {
        [JsonConstructor]
        public RawCoords(double x, double y, double z) {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; init; }
        public double Y { get; init; }
        public double Z { get; init; }
    }

    public override void WriteJson(JsonWriter writer, Point? value, JsonSerializer serializer) {
        if (value is null) {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.X);
        writer.WritePropertyName("y");
        writer.WriteValue(value.Y);
        writer.WritePropertyName("z");
        writer.WriteValue(value.Z);
        writer.WriteEndObject();
    }

    public override Point? ReadJson(
        JsonReader reader,
        Type objectType,
        Point? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    ) {
        var c = existingValue ?? new Point(0, 0, 0);
        var rc = serializer.Deserialize<RawCoords?>(reader);
        if (rc is null) {
            return null;
        }

        c.X = rc.X;
        c.Y = rc.Y;
        c.Z = rc.Z;
        return c;
    }
}

public class OneHotGenuses {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong BodyId64 { get; set; }

    public Body Body { get; set; }
    public bool Aleoids { get; set; }
    public bool Bacterial { get; set; }
    public bool Cactoid { get; set; }
    public bool Clypeus { get; set; }
    public bool Conchas { get; set; }
    public bool Electricae { get; set; }
    public bool Fonticulus { get; set; }
    public bool Fumerolas { get; set; }
    public bool Fungoids { get; set; }
    public bool Osseus { get; set; }
    public bool Recepta { get; set; }
    public bool Shrubs { get; set; }
    public bool Stratum { get; set; }
    public bool Tubus { get; set; }
    public bool Tussocks { get; set; }
}


// [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
// [PrimaryKey(nameof(BodyId64), nameof(RawId))]
// public class Station : IUpsertHandler, IComparable<Station> {
//     [JsonConstructor]
//     public Station(ulong rawId, string name, DateTime lastUpdated) {
//         RawId = rawId;
//         Name = name;
//         LastUpdated = lastUpdated;
//     }
//
//     [JsonIgnore] public ulong BodyId64 { get; set; }
//
//     [JsonIgnore] public Body Body { get; set; } = null!;
//
//     public Station(Station station) : this(station.RawId, station.Name, station.LastUpdated) { }
//
//     [JsonProperty(PropertyName = "id", Required = Required.Always)]
//     public ulong RawId { get; set; }
//
//     [JsonProperty(Required = Required.Always)]
//     public string Name { get; set; }
//
//     [JsonProperty(PropertyName = "updateTime", Required = Required.Always)]
//     [JsonConverter(typeof(IsoDateTimeConverter))]
//     public DateTime LastUpdated { get; set; }
//
//     public void HandleUpsertChildren(EEUContext ctx) { }
//
//     public int CompareTo(Station? other) {
//         if (ReferenceEquals(this, other)) {
//             return 0;
//         }
//
//         if (ReferenceEquals(null, other)) {
//             return 1;
//         }
//
//         var bodyId64Comparison = BodyId64.CompareTo(other.BodyId64);
//         if (bodyId64Comparison != 0) {
//             return bodyId64Comparison;
//         }
//
//         var rawIdComparison = RawId.CompareTo(other.RawId);
//         if (rawIdComparison != 0) {
//             return rawIdComparison;
//         }
//
//         var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
//         if (nameComparison != 0) {
//             return nameComparison;
//         }
//
//         return LastUpdated.CompareTo(other.LastUpdated);
//     }
// }
