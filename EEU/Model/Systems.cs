using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EEU.Utils.Init;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Nullable.Extensions;

namespace EEU.Model;

[Serializable]
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
[Index(nameof(Name), IsUnique = true)]
public class System {
    public System(ulong id64, string name, Point coords, DateTime updated) {
        Id64 = id64;
        Name = name;
        Coords = coords;
        Updated = updated;
    }

    [Key]
    [JsonProperty(Required = Required.Always)]
    public ulong Id64 { get; set; }

    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; }

    [JsonProperty(Required = Required.Always)]
    [JsonConverter(typeof(PointConverter))]
    [Column(TypeName = "POINTZ")]
    public Point Coords { get; set; }

    public ulong? Population { get; set; }

    public ulong? BodyCount { get; set; }

    [JsonProperty(PropertyName = "date", Required = Required.Always)]
    [JsonConverter(typeof(IsoDateTimeConverter))]
    public DateTime Updated { get; set; }

    [JsonProperty(Required = Required.Always)]
    public List<Body> Bodies { get; set; } = new();

    [JsonProperty(Required = Required.Always)]
    public List<SystemStation> Stations { get; set; } = new();
}

[Serializable]
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(SystemId64), nameof(BodyId))]
public class Body {
    public Body(ulong id64, ulong bodyId, string type, string name, DateTime lastUpdated) {
        Id64 = id64;
        BodyId = bodyId;
        Type = type;
        Name = name;
        LastUpdated = lastUpdated;
    }

    [JsonIgnore] public ulong SystemId64 { get; set; }
    [JsonIgnore] [ForeignKey("SystemId64")] public System System { get; set; } = null!;

    [Key]
    [JsonProperty(Required = Required.Always)]
    public ulong Id64 { get; set; }

    [JsonProperty(Required = Required.Always)]
    public ulong BodyId { get; set; }

    [JsonProperty(Required = Required.Always)]
    public string Type { get; set; }

    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; }

    public double? SemiMajorAxis { get; set; }
    public double? OrbitalEccentricity { get; set; }
    public double? OrbitalInclination { get; set; }
    public double? AgeOfPeriapsis { get; set; }
    public double? MeanAnomaly { get; set; }
    public double? AscendingNode { get; set; }

    [JsonProperty(Required = Required.Always)]
    public List<BodyStation> Stations { get; set; }

    [JsonProperty(Required = Required.Always, PropertyName = "updateTime")]
    [JsonConverter(typeof(IsoDateTimeConverter))]
    public DateTime LastUpdated { get; set; }

    public string? SubType { get; set; }

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

    public List<Parent> Parents { get; set; } = new();

    public bool? IsLandable { get; set; }

    public double? Gravity { get; set; }

    public double? EarthMasses { get; set; }

    public double? Radius { get; set; }

    public double? SurfacePressure { get; set; }

    public string? AtmosphereType { get; set; }

    public AtmosphereComposition? AtmosphereComposition { get; set; }

    public string? TerraformingState { get; set; }

    public string? VolcanismType { get; set; }

    public SolidComposition? SolidComposition { get; set; }

    public Materials? Materials { get; set; }

    public Signals? Signals { get; set; }

    public string? ReserveLevel { get; set; }
    
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class Signals {
    [JsonConstructor]
    public Signals(DateTime lastUpdated) {
        LastUpdated = lastUpdated;
    }
    
    [JsonIgnore] public ulong BodyId64 { get; set; }

    [JsonIgnore] public Body Body { get; set; } = null!;

    [JsonIgnore] public long Id { get; set; }

    [JsonProperty(PropertyName = "signals", Required = Required.Always)]
    public Detections Detected { get; set; } = null!;

    [Owned]
    [JsonObject]
    public class Detections {
        [JsonProperty(PropertyName = "$SAA_SignalType_Human;")]
        public long? Human;

        [JsonProperty(PropertyName = "$SAA_SignalType_Biological;")]
        public long? Biological;

        [JsonProperty(PropertyName = "$SAA_SignalType_Geological;")]
        public long? Geological;

        [JsonProperty(PropertyName = "$SAA_SignalType_Other;")]
        public long? Other;

        [JsonProperty(PropertyName = "$SAA_SignalType_Thargoid;")]
        public long? Thargoid;

        [JsonProperty(PropertyName = "$SAA_SignalType_Guardian;")]
        public long? Guardian;

        public long? Painite;
        public long? Platinum;
        public long? Rhodplumsite;
        public long? Serendibite;
        public long? Alexandrite;
        public long? Benitoite;
        public long? Monazite;
        public long? Musgravite;
    }

    [JsonProperty(PropertyName = "updateTime", Required = Required.Always)]
    [JsonConverter(typeof(IsoDateTimeConverter))]
    public DateTime LastUpdated { get; set; }

    public List<Genus> Genuses { get; set; } = new();
}

[JsonConverter(typeof(Converter))]
public class Genus {
    public Genus(string name) {
        Name = name;
    }

    public Signals Signals { get; set; } = null!;

    public string Name { get; set; }

    public long Id { get; set; }

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
}

public class Materials {
    [JsonIgnore] public long Id { get; set; }

    [JsonIgnore] public ulong BodyId64 { get; set; }
    [JsonIgnore] public Body Body { get; set; } = null!;

    public double? Antimony { get; set; }
    public double? Arsenic { get; set; }
    public double? Carbon { get; set; }

    [JsonProperty(Required = Required.Always)]
    public double Iron { get; set; }

    [JsonProperty(Required = Required.Always)]
    public double Nickel { get; set; }

    public double? Niobium { get; set; }
    public double? Phosphorus { get; set; }
    public double? Sulphur { get; set; }
    public double? Tin { get; set; }
    public double? Zinc { get; set; }
    public double? Zirconium { get; set; }
    public double? Cadmium { get; set; }
    public double? Manganese { get; set; }
    public double? Mercury { get; set; }
    public double? Tellurium { get; set; }
    public double? Vanadium { get; set; }
    public double? Chromium { get; set; }
    public double? Germanium { get; set; }
    public double? Molybdenum { get; set; }
    public double? Ruthenium { get; set; }
    public double? Yttrium { get; set; }
    public double? Selenium { get; set; }
    public double? Technetium { get; set; }
    public double? Tungsten { get; set; }
    public double? Polonium { get; set; }
}

[JsonObject(ItemRequired = Required.Always)]
public class SolidComposition {
    [JsonIgnore] public long Id { get; set; }
    [JsonIgnore] public ulong BodyId64 { get; set; }
    [JsonIgnore] public Body Body { get; set; } = null!;

    public double Ice { get; set; }
    public double Metal { get; set; }
    public double Rock { get; set; }
}

[JsonObject(ItemRequired = Required.DisallowNull)]
public class AtmosphereComposition {
    [JsonIgnore] public long Id { get; set; }

    [JsonIgnore] public ulong BodyId64 { get; set; }
    [JsonIgnore] public Body Body { get; set; } = null!;

    public double? Helium { get; set; }
    public double? Hydrogen { get; set; }

    [JsonProperty(PropertyName = "Carbon dioxide")]
    public double? CarbonDioxide { get; set; }

    public double? Silicates { get; set; }

    [JsonProperty(PropertyName = "Sulphur dioxide")]
    public double? SulphurDioxide { get; set; }

    public double? Nitrogen { get; set; }
    public double? Neon { get; set; }
    public double? Iron { get; set; }
    public double? Argon { get; set; }
    public double? Ammonia { get; set; }
    public double? Methane { get; set; }
    public double? Water { get; set; }
    public double? Oxygen { get; set; }
}

[JsonConverter(typeof(Converter))]
public class Parent {
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Type {
        Null,
        Star,
        Planet,
    }

    public long Id { get; set; }

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

    public Body Body { get; set; } = null!;

    public ulong Local { get; set; }
    
    public Type Kind { get; set; }
}

public static class TypeSupport {
    public static string ToString(this Parent.Type t) {
        return t switch {
            Parent.Type.Null   => "Null",
            Parent.Type.Star   => "Star",
            Parent.Type.Planet => "Planet",
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

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class Station {
#pragma warning disable CS8618
    public Station(Station station) {
        ReflectInit<Station>.From(this, station);
    }
#pragma warning restore CS8618

    [JsonConstructor]
    public Station(ulong id, string name, DateTime lastUpdated) {
        Id = id;
        Name = name;
        LastUpdated = lastUpdated;
    }

    [Key]
    [JsonProperty(Required = Required.Always)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "updateTime", Required = Required.Always)]
    [JsonConverter(typeof(IsoDateTimeConverter))]
    public DateTime LastUpdated { get; set; }
}

[JsonConverter(typeof(Converter))]
public class BodyStation : Station {
    public BodyStation(ulong id, string name, DateTime lastUpdated) : base(id, name, lastUpdated) { }

    public Body Body { get; set; } = null!;
    public BodyStation(Station station) : base(station) { }

    private class Converter : JsonConverter<BodyStation> {
        public override void WriteJson(JsonWriter writer, BodyStation? value, JsonSerializer serializer) {
            if (value is null) {
                serializer.Serialize(writer, null);
                return;
            }

            serializer.Serialize(writer, new Station(value));
        }

        public override BodyStation? ReadJson(JsonReader reader,
            Type objectType,
            BodyStation? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) {
            var st = serializer.Deserialize<Station>(reader);
            return st.Bind(s => new BodyStation(s));
        }
    }
}

[JsonConverter(typeof(Converter))]
public class SystemStation : Station {
    public SystemStation(Station station) : base(station) { }

    public SystemStation(ulong id, string name, DateTime lastUpdated) : base(id, name, lastUpdated) { }

    public System System { get; set; } = null!;

    public class Converter : JsonConverter<SystemStation> {
        public override void WriteJson(JsonWriter writer, SystemStation? value, JsonSerializer serializer) {
            if (value is null) {
                serializer.Serialize(writer, null);
                return;
            }

            serializer.Serialize(writer, new Station(value));
        }

        public override SystemStation? ReadJson(JsonReader reader,
            Type objectType,
            SystemStation? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) {
            var st = serializer.Deserialize<Station>(reader);
            return st.Bind(s => new SystemStation(s));
        }
    }
}
