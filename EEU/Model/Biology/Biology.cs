// EliteExplorationUtility - EEU - Biology.cs
// Copyright (C) 2023 Nick Samson
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using EEU.Utils;
using EEU.Utils.Conversions;
using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EEU.Model.Biology;

[PrimaryKey(nameof(BodyName), nameof(Genus), nameof(Species), nameof(SystemId64))]
[Microsoft.EntityFrameworkCore.Index(nameof(Genus), nameof(Species))]
public class Biology : IUpsertHandler, IEquatable<Biology>, IComparable<Biology> {
    [Column(TypeName = "nvarchar(250)")] public string BodyName { get; set; }
    [Column(TypeName = "nvarchar(100)")] public string Genus { get; set; }
    [Column(TypeName = "nvarchar(100)")] public string Species { get; set; }

    public ulong SystemId64 { get; set; }

    public int CompareTo(Biology? other) {
        if (ReferenceEquals(this, other)) {
            return 0;
        }

        if (ReferenceEquals(null, other)) {
            return 1;
        }

        var bodyNameComparison = string.Compare(BodyName, other.BodyName, StringComparison.Ordinal);
        if (bodyNameComparison != 0) {
            return bodyNameComparison;
        }

        var genusComparison = string.Compare(Genus, other.Genus, StringComparison.Ordinal);
        if (genusComparison != 0) {
            return genusComparison;
        }

        var speciesComparison = string.Compare(Species, other.Species, StringComparison.Ordinal);
        if (speciesComparison != 0) {
            return speciesComparison;
        }

        return SystemId64.CompareTo(other.SystemId64);
    }

    public bool Equals(Biology? other) {
        if (ReferenceEquals(null, other)) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return SystemId64 == other.SystemId64 && BodyName == other.BodyName && Genus == other.Genus && Species == other.Species;
    }

    public void HandleUpsertChildren(EEUContext ctx, BulkConfig config) { }

    public async Task HandleUpsertChildrenAsync(EEUContext ctx, BulkConfig config) { }

    public void PrepareForUpsert() { }

    public void GatherChildEntities(TypeEntityMapping tem) { }

    private static Biology? FromRaw(Raw r) {
        if (r.Body.IsNullOrEmpty()) {
            return null;
        }

        var parts = r.Name.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return new Biology {
            SystemId64 = r.Id64.ParseULong(),
            BodyName = r.Body,
            Genus = parts[0].ToLower(),
            Species = parts[1].ToLower(),
        };
    }

    public static IEnumerable<Biology> FromCsv(CsvReader rd) {
        return rd.GetRecords<Raw>()
            .Select(FromRaw)
            .Where(x => x is not null)
            .Distinct()!;
    }

    public static IEnumerable<Biology> FromCsvDirectory(string path) {
        var dirWalk = new DirectoryInfo(path);
        foreach (var file in dirWalk.EnumerateFiles()) {
            if (!file.Extension.EndsWith("csv")) {
                continue;
            }

            if (!file.Exists) {
                continue;
            }

            var parser = new CsvParser(
                new StreamReader(new FileStream(file.FullName, FileMode.Open)),
                new CsvConfiguration(CultureInfo.CurrentCulture) {
                    TrimOptions = TrimOptions.Trim,
                }
            );
            using var rd = new CsvReader(parser);
            foreach (var bio in FromCsv(rd)) {
                yield return bio;
            }
        }
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) {
            return false;
        }

        if (ReferenceEquals(this, obj)) {
            return true;
        }

        if (obj.GetType() != GetType()) {
            return false;
        }

        return Equals((Biology) obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(SystemId64, BodyName, Genus, Species);
    }

    private class Raw {
        public string Id64 { get; set; }

        public string? Body { get; set; }
        public string Name { get; set; }
    }
}

[PrimaryKey(nameof(Genus), nameof(Species))]
public class SpeciesInformation {
    [Column(TypeName = "nvarchar(100)")] public string Genus { set; get; }
    [Column(TypeName = "nvarchar(100)")] public string Species { set; get; }

    public long Value { get; set; }

    public long ClonalRange { get; set; }

    public static IEnumerable<SpeciesInformation> FromLimitedCsv(CsvReader rd) {
        return rd.GetRecords<LimitedSpeciesInformation>().Select(x => x.ToSpeciesInformation());
    }

    public static IEnumerable<SpeciesInformation> FromCsv(CsvReader rd) {
        return rd.GetRecords<SpeciesInformation>();
    }

    public static void UpsertSpecies(SqlConnection conn, IEnumerable<SpeciesInformation> species) {
        using var tx = conn.BeginTransaction();
        using var cmd = new SqlCommand(
            @"
                MERGE INTO Species WITH (HOLDLOCK) AS Tgt 
                USING (SELECT @Genus AS Genus, @Species AS Species, @Value AS Value, @ClonalRange AS ClonalRange) AS Src
                ON Tgt.Genus = Src.Genus AND Tgt.Species = Src.Species
                WHEN MATCHED THEN
                    UPDATE SET Tgt.Value = Src.Value, Tgt.ClonalRange = Src.ClonalRange
                WHEN NOT MATCHED THEN
                    INSERT (Genus, Species, Value, ClonalRange) VALUES (Src.Genus, Src.Species, Src.Value, Src.ClonalRange);
            ",
            conn,
            tx
        );
        foreach (var s in species) {
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Genus", s.Genus);
            cmd.Parameters.AddWithValue("@Species", s.Species);
            cmd.Parameters.AddWithValue("@Value", s.Value);
            cmd.Parameters.AddWithValue("@ClonalRange", s.ClonalRange);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    private class LimitedSpeciesInformation {
        [Name("Species")] public string Species { get; set; }
        [Name("Value")] public long Value { get; set; }

        public SpeciesInformation ToSpeciesInformation() {
            var parts = Species.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return new SpeciesInformation {
                Genus = parts[0].ToLower(),
                Species = parts[1].ToLower(),
                Value = Value,
                ClonalRange = 0,
            };
        }
    }
}
