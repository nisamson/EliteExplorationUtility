// ReSharper disable InconsistentNaming

using System.Diagnostics;
using System.Runtime.InteropServices;
using EEU.Utils;
using EEU.Utils.Init;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace EEU.Model;

public class EEUContext : DbContext {
    private static readonly Once loadLibrary = new(delegate { Ensure.Loadable(package: "mod_spatialite", library: "mod_spatialite"); });
    public EEUContext(DbContextOptions<EEUContext> options) : base(options) { }

    public class DesignFactory : IDesignTimeDbContextFactory<EEUContext> {
        public EEUContext CreateDbContext(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<EEUContext>();
            var b = new SqliteConnectionStringBuilder
                { ForeignKeys = true, DataSource = "./design.sqlite3", Pooling = false, BrowsableConnectionString = true };
            optionsBuilder.UseLoggerFactory(new LoggerFactory())
                // .EnableSensitiveDataLogging()
                // .EnableDetailedErrors()
                // .LogTo(Console.WriteLine)
                .UseSqlite(b.ConnectionString);

            return new EEUContext(optionsBuilder.Options);
        }
    }

    public EEUContext() { }

    public DbSet<System> Systems { get; set; } = null!;
    public DbSet<Body> Bodies { get; set; } = null!;
    public DbSet<Station> Stations { get; set; } = null!;
    public DbSet<Signals> Signals { get; set; } = null!;
    public DbSet<Genus> Genera { get; set; } = null!;
    public DbSet<Materials> Materials { get; set; }
    public DbSet<SolidComposition> SolidCompositions { get; set; }
    public DbSet<AtmosphereComposition> AtmosphereCompositions { get; set; }
    public DbSet<Parent> Parents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        loadLibrary.Init();
        optionsBuilder.UseSqlite(sql => sql.UseNetTopologySuite());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) { }
}
