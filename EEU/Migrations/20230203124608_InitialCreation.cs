using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace EEU.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Sqlite:InitSpatialMetaData", true);

            migrationBuilder.CreateTable(
                name: "Systems",
                columns: table => new
                {
                    Id64 = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Coords = table.Column<Point>(type: "POINTZ", nullable: false),
                    Population = table.Column<ulong>(type: "INTEGER", nullable: true),
                    BodyCount = table.Column<ulong>(type: "INTEGER", nullable: true),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Systems", x => x.Id64);
                });

            migrationBuilder.CreateTable(
                name: "Bodies",
                columns: table => new
                {
                    Id64 = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SystemId64 = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BodyId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SemiMajorAxis = table.Column<double>(type: "REAL", nullable: true),
                    OrbitalEccentricity = table.Column<double>(type: "REAL", nullable: true),
                    OrbitalInclination = table.Column<double>(type: "REAL", nullable: true),
                    AgeOfPeriapsis = table.Column<double>(type: "REAL", nullable: true),
                    MeanAnomaly = table.Column<double>(type: "REAL", nullable: true),
                    AscendingNode = table.Column<double>(type: "REAL", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SubType = table.Column<string>(type: "TEXT", nullable: true),
                    DistanceToArrival = table.Column<double>(type: "REAL", nullable: true),
                    MainStar = table.Column<bool>(type: "INTEGER", nullable: true),
                    Age = table.Column<long>(type: "INTEGER", nullable: true),
                    SpectralClass = table.Column<string>(type: "TEXT", nullable: true),
                    Luminosity = table.Column<string>(type: "TEXT", nullable: true),
                    AbsoluteMagnitude = table.Column<double>(type: "REAL", nullable: true),
                    SolarMasses = table.Column<double>(type: "REAL", nullable: true),
                    SolarRadius = table.Column<double>(type: "REAL", nullable: true),
                    SurfaceTemperature = table.Column<double>(type: "REAL", nullable: true),
                    RotationalPeriod = table.Column<double>(type: "REAL", nullable: true),
                    RotationalPeriodTidallyLocked = table.Column<bool>(type: "INTEGER", nullable: true),
                    AxialTilt = table.Column<double>(type: "REAL", nullable: true),
                    IsLandable = table.Column<bool>(type: "INTEGER", nullable: true),
                    Gravity = table.Column<double>(type: "REAL", nullable: true),
                    EarthMasses = table.Column<double>(type: "REAL", nullable: true),
                    Radius = table.Column<double>(type: "REAL", nullable: true),
                    SurfacePressure = table.Column<double>(type: "REAL", nullable: true),
                    AtmosphereType = table.Column<string>(type: "TEXT", nullable: true),
                    TerraformingState = table.Column<string>(type: "TEXT", nullable: true),
                    VolcanismType = table.Column<string>(type: "TEXT", nullable: true),
                    ReserveLevel = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bodies", x => x.Id64);
                    table.ForeignKey(
                        name: "FK_Bodies_Systems_SystemId64",
                        column: x => x.SystemId64,
                        principalTable: "Systems",
                        principalColumn: "Id64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtmosphereCompositions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BodyId64 = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Helium = table.Column<double>(type: "REAL", nullable: true),
                    Hydrogen = table.Column<double>(type: "REAL", nullable: true),
                    CarbonDioxide = table.Column<double>(type: "REAL", nullable: true),
                    Silicates = table.Column<double>(type: "REAL", nullable: true),
                    SulphurDioxide = table.Column<double>(type: "REAL", nullable: true),
                    Nitrogen = table.Column<double>(type: "REAL", nullable: true),
                    Neon = table.Column<double>(type: "REAL", nullable: true),
                    Iron = table.Column<double>(type: "REAL", nullable: true),
                    Argon = table.Column<double>(type: "REAL", nullable: true),
                    Ammonia = table.Column<double>(type: "REAL", nullable: true),
                    Methane = table.Column<double>(type: "REAL", nullable: true),
                    Water = table.Column<double>(type: "REAL", nullable: true),
                    Oxygen = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtmosphereCompositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtmosphereCompositions_Bodies_BodyId64",
                        column: x => x.BodyId64,
                        principalTable: "Bodies",
                        principalColumn: "Id64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BodyId64 = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Antimony = table.Column<double>(type: "REAL", nullable: true),
                    Arsenic = table.Column<double>(type: "REAL", nullable: true),
                    Carbon = table.Column<double>(type: "REAL", nullable: true),
                    Iron = table.Column<double>(type: "REAL", nullable: false),
                    Nickel = table.Column<double>(type: "REAL", nullable: false),
                    Niobium = table.Column<double>(type: "REAL", nullable: true),
                    Phosphorus = table.Column<double>(type: "REAL", nullable: true),
                    Sulphur = table.Column<double>(type: "REAL", nullable: true),
                    Tin = table.Column<double>(type: "REAL", nullable: true),
                    Zinc = table.Column<double>(type: "REAL", nullable: true),
                    Zirconium = table.Column<double>(type: "REAL", nullable: true),
                    Cadmium = table.Column<double>(type: "REAL", nullable: true),
                    Manganese = table.Column<double>(type: "REAL", nullable: true),
                    Mercury = table.Column<double>(type: "REAL", nullable: true),
                    Tellurium = table.Column<double>(type: "REAL", nullable: true),
                    Vanadium = table.Column<double>(type: "REAL", nullable: true),
                    Chromium = table.Column<double>(type: "REAL", nullable: true),
                    Germanium = table.Column<double>(type: "REAL", nullable: true),
                    Molybdenum = table.Column<double>(type: "REAL", nullable: true),
                    Ruthenium = table.Column<double>(type: "REAL", nullable: true),
                    Yttrium = table.Column<double>(type: "REAL", nullable: true),
                    Selenium = table.Column<double>(type: "REAL", nullable: true),
                    Technetium = table.Column<double>(type: "REAL", nullable: true),
                    Tungsten = table.Column<double>(type: "REAL", nullable: true),
                    Polonium = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Materials_Bodies_BodyId64",
                        column: x => x.BodyId64,
                        principalTable: "Bodies",
                        principalColumn: "Id64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BodyId64 = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Local = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parents_Bodies_BodyId64",
                        column: x => x.BodyId64,
                        principalTable: "Bodies",
                        principalColumn: "Id64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Signals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BodyId64 = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Signals_Bodies_BodyId64",
                        column: x => x.BodyId64,
                        principalTable: "Bodies",
                        principalColumn: "Id64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolidCompositions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BodyId64 = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Ice = table.Column<double>(type: "REAL", nullable: false),
                    Metal = table.Column<double>(type: "REAL", nullable: false),
                    Rock = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolidCompositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolidCompositions_Bodies_BodyId64",
                        column: x => x.BodyId64,
                        principalTable: "Bodies",
                        principalColumn: "Id64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    BodyId64 = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SystemId64 = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stations_Bodies_BodyId64",
                        column: x => x.BodyId64,
                        principalTable: "Bodies",
                        principalColumn: "Id64",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Stations_Systems_SystemId64",
                        column: x => x.SystemId64,
                        principalTable: "Systems",
                        principalColumn: "Id64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Genera",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SignalsId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genera", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Genera_Signals_SignalsId",
                        column: x => x.SignalsId,
                        principalTable: "Signals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtmosphereCompositions_BodyId64",
                table: "AtmosphereCompositions",
                column: "BodyId64",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bodies_Name",
                table: "Bodies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bodies_SystemId64_BodyId",
                table: "Bodies",
                columns: new[] { "SystemId64", "BodyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Genera_SignalsId",
                table: "Genera",
                column: "SignalsId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_BodyId64",
                table: "Materials",
                column: "BodyId64",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parents_BodyId64",
                table: "Parents",
                column: "BodyId64");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_BodyId64",
                table: "Signals",
                column: "BodyId64",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolidCompositions_BodyId64",
                table: "SolidCompositions",
                column: "BodyId64",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stations_BodyId64",
                table: "Stations",
                column: "BodyId64");

            migrationBuilder.CreateIndex(
                name: "IX_Stations_SystemId64",
                table: "Stations",
                column: "SystemId64");

            migrationBuilder.CreateIndex(
                name: "IX_Systems_Name",
                table: "Systems",
                column: "Name",
                unique: true);
            migrationBuilder.Sql("SELECT CreateSpatialIndex('Systems', 'Coords');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtmosphereCompositions");

            migrationBuilder.DropTable(
                name: "Genera");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropTable(
                name: "SolidCompositions");

            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropTable(
                name: "Signals");

            migrationBuilder.DropTable(
                name: "Bodies");

            migrationBuilder.DropTable(
                name: "Systems");
            migrationBuilder.Sql("SELECT DisableSpatialIndex('Systems', 'Coords');");
            migrationBuilder.Sql("DROP TABLE idx_Systems_Geometry;");
        }
    }
}
