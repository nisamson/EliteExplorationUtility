// EliteExplorationUtility - EEU - EEUContext.cs
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

// ReSharper disable InconsistentNaming

using EEU.Model.Biology;
using EEU.Utils;
using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace EEU.Model;

public class EEUContext : DbContext {
    public const string DefaultConnString =
        "Server=localhost;Database=master;Trusted_Connection=True;Trust Server Certificate=true;Command Timeout=0;";

    public EEUContext(DbContextOptions<EEUContext> options) : base(options) { }

    public EEUContext() { }
    // migrationBuilder.Sql(@"SELECT CreateSpatialIndex('Systems', 'Coords');");
    // migrationBuilder.Sql(@"SELECT DisableSpatialIndex('Systems', 'Coords');");
    // migrationBuilder.DropTable(name: "idx_Systems_Coords");

    /*protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SELECT CreateSpatialIndex('Systems', 'Coords');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SELECT DisableSpatialIndex('Systems', 'Coords');");
            migrationBuilder.DropTable(name: "idx_Systems_Coords");
        }*/

    public DbSet<System> Systems { get; set; } = null!;

    public DbSet<Body> Bodies { get; set; } = null!;

    // public DbSet<Station> Stations { get; set; } = null!;
    public DbSet<Signals> Signals { get; set; } = null!;
    public DbSet<Genus> Genera { get; set; } = null!;
    public DbSet<Parent> Parents { get; set; }
    public DbSet<AtmosphereComposition> AtmosphereCompositions { get; set; }
    public DbSet<SolidComposition> SolidCompositions { get; set; }
    public DbSet<Materials> Materials { get; set; }
    public DbSet<Biology.Biology> Biology { get; set; }
    public DbSet<SpeciesInformation> Species { get; set; }
    public DbSet<OneHotGenuses> OneHotGenera { get; set; }
    public DbSet<CodexEntry> CodexEntries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        // optionsBuilder.UseSqlServer(s => s.EnableRetryOnFailure(15));
        optionsBuilder.UseValidationCheckConstraints();
    }

    internal void PrepareUpsert(IList<System> systems, BulkConfig? config) {
        var keyedSys = systems.ToDictionary(x => x.Id64);
        Systems.Where(x => keyedSys.Keys.Contains(x.Id64)).ExecuteDelete();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        // body havers
        // foreach (var (t, prop) in new[] {
        //              (typeof(AtmosphereComposition), "AtmosphereComposition"),
        //              (typeof(SolidComposition), "SolidComposition"),
        //              (typeof(Signals), "Signals"),
        //              (typeof(Materials), "Materials"),
        //          }) {
        //     modelBuilder.Entity(t)
        //         .Property<ulong>("BodyId64");
        //
        //     modelBuilder.Entity<Body>()
        //         .HasOne(t, prop)
        //         .WithOne("Body")
        //         .HasForeignKey(prop, "BodyId64");
        // }
        //
        // foreach (var (t, prop) in new[] { (typeof(Parent), "Parents"), (typeof(Station), "Stations") }) {
        //     modelBuilder.Entity(t)
        //         .Property<ulong>("BodyId64");
        //
        //     modelBuilder.Entity(t)
        //         .HasOne("Body")
        //         .WithMany(prop)
        //         .HasForeignKey("BodyId64");
        // }
        //
        // modelBuilder.Entity<Body>()
        //     .Property<ulong>("SystemId64");
        //
        // modelBuilder.Entity<System>()
        //     .HasMany<Body>("Bodies")
        //     .WithOne("System")
        //     .HasForeignKey("SystemId64");
        modelBuilder.Entity<Genus>()
            .HasOne(a => a.Signals)
            .WithMany(a => a.Genuses)
            .HasForeignKey(c => c.SignalsBodyId64);

        modelBuilder.Entity<Parent>()
            .HasOne(a => a.Body)
            .WithMany(a => a.Parents)
            .HasForeignKey(c => c.BodyId64);

        modelBuilder.Entity<Body>()
            .Property(b => b.Id64)
            .ValueGeneratedNever();
        modelBuilder.Entity<Body>()
            .HasKey(b => b.Id64)
            .IsClustered(false);
        modelBuilder.Entity<Body>()
            .HasIndex(b => new { b.Name })
            .IsClustered();
        modelBuilder.Entity<Body>()
            .HasIndex(b => new { b.Type, b.IsLandable })
            .IncludeProperties(b => new { b.Name, b.SubType })
            .HasFilter("[Type] IS NOT NULL");
        modelBuilder.Entity<Body>()
            .HasIndex(b => b.IsLandable)
            .IncludeProperties(b => new { b.Name, b.Type, b.SubType })
            .HasFilter("[Type] IS NOT NULL");

        modelBuilder.Entity<System>()
            .Property(s => s.Id64)
            .ValueGeneratedNever();
        modelBuilder.Entity<System>()
            .HasKey(s => s.Id64)
            .IsClustered(false);
        modelBuilder.Entity<System>()
            .HasIndex(s => s.Name)
            .IsClustered();

        modelBuilder.Entity<OneHotGenuses>()
            .ToView(nameof(OneHotGenera))
            .HasOne(x => x.Body)
            .WithOne()
            .HasForeignKey(nameof(OneHotGenuses), "BodyId64");

        modelBuilder.Entity<CodexEntry>()
            .Property(x => x.ValueHash)
            .HasComputedColumnSql(@"CONVERT(varbinary(64), HASHBYTES('SHA2_512', Value))", true)
            .ValueGeneratedNever();

        modelBuilder.Entity<CodexEntry>()
            .HasKey(x => x.ValueHash);
        modelBuilder.Entity<CodexEntry>()
            .ToTable(t => t.HasCheckConstraint("CK_Value_ValidJson", "ISJSON([Value]) > 0"));


        // modelBuilder.Entity<Signals>()
        //     .OwnsOne<Detections>(x => x.Detections, builder => { });

        // modelBuilder.Entity<System>()
        //     .OwnsOne<Coords>(
        //         s => s.Coords,
        //         c => {
        //             c.Property(x => x.X).HasColumnName("X");
        //             c.Property(x => x.Y).HasColumnName("Y");
        //             c.Property(x => x.Z).HasColumnName("Z");
        //         }
        //     );
        //
        // modelBuilder.Entity<Signals>()
        //     .OwnsOne<Detections>(d => d.Detected)
        //     .WithOwner();


        // modelBuilder.Entity<Station>()
        //     // .HasOne(a => a.Body)
        //     // .WithMany(a => a.Stations)
        //     // .HasForeignKey(c => c.BodyId64);

        // modelBuilder.Entity<Body>()
        //     .Property<ulong>("SystemId64");
        //
        // modelBuilder.Entity<Body>()
        //     .HasOne(s => s.System)
        //     .WithMany(s => s.Bodies)
        //     .HasForeignKey("SystemId64");
    }

    public void LoadCodexJson(TextReader rd) {
        using var tx = Database.BeginTransaction();

        foreach (var entry in rd.Lines()) {
            try {
                Database.ExecuteSqlRaw(@"dbo.AddCodexEntry @val", new SqlParameter("@val", entry));
            } catch (Exception) {
                Console.WriteLine(entry);
                throw;
            }
        }

        tx.Commit();
    }

    public class DesignFactory : IDesignTimeDbContextFactory<EEUContext> {
        public EEUContext CreateDbContext(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<EEUContext>();
            optionsBuilder.UseLoggerFactory(new LoggerFactory())
                // .EnableSensitiveDataLogging()
                // .EnableDetailedErrors()
                // .LogTo(Console.WriteLine)
                .UseSqlServer(DefaultConnString);

            return new EEUContext(optionsBuilder.Options);
        }
    }
}
