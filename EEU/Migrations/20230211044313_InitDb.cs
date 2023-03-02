using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EEU.Migrations
{
    /// <inheritdoc />
    public partial class InitDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Biology",
                columns: table => new
                {
                    SystemId64 = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    BodyName = table.Column<string>(type: "nvarchar(250)", nullable: false),
                    Genus = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Species = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Biology", x => new { x.BodyName, x.Genus, x.Species, x.SystemId64 });
                });

            migrationBuilder.CreateTable(
                name: "Systems",
                columns: table => new
                {
                    Id64 = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", nullable: false),
                    CoordsX = table.Column<double>(name: "Coords_X", type: "float", nullable: false),
                    CoordsY = table.Column<double>(name: "Coords_Y", type: "float", nullable: false),
                    CoordsZ = table.Column<double>(name: "Coords_Z", type: "float", nullable: false),
                    Population = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    BodyCount = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Systems", x => x.Id64)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateTable(
                name: "Bodies",
                columns: table => new
                {
                    Id64 = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    SystemId64 = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    BodyId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SemiMajorAxis = table.Column<double>(type: "float", nullable: true),
                    OrbitalEccentricity = table.Column<double>(type: "float", nullable: true),
                    OrbitalInclination = table.Column<double>(type: "float", nullable: true),
                    AgeOfPeriapsis = table.Column<double>(type: "float", nullable: true),
                    MeanAnomaly = table.Column<double>(type: "float", nullable: true),
                    AscendingNode = table.Column<double>(type: "float", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubType = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DistanceToArrival = table.Column<double>(type: "float", nullable: true),
                    MainStar = table.Column<bool>(type: "bit", nullable: true),
                    Age = table.Column<long>(type: "bigint", nullable: true),
                    SpectralClass = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Luminosity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AbsoluteMagnitude = table.Column<double>(type: "float", nullable: true),
                    SolarMasses = table.Column<double>(type: "float", nullable: true),
                    SolarRadius = table.Column<double>(type: "float", nullable: true),
                    SurfaceTemperature = table.Column<double>(type: "float", nullable: true),
                    RotationalPeriod = table.Column<double>(type: "float", nullable: true),
                    RotationalPeriodTidallyLocked = table.Column<bool>(type: "bit", nullable: true),
                    AxialTilt = table.Column<double>(type: "float", nullable: true),
                    IsLandable = table.Column<bool>(type: "bit", nullable: false),
                    Gravity = table.Column<double>(type: "float", nullable: true),
                    EarthMasses = table.Column<double>(type: "float", nullable: true),
                    Radius = table.Column<double>(type: "float", nullable: true),
                    SurfacePressure = table.Column<double>(type: "float", nullable: true),
                    AtmosphereType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TerraformingState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VolcanismType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReserveLevel = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bodies", x => x.Id64)
                        .Annotation("SqlServer:Clustered", false);
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
                    BodyId64 = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Helium = table.Column<double>(type: "float", nullable: false),
                    Hydrogen = table.Column<double>(type: "float", nullable: false),
                    CarbonDioxide = table.Column<double>(type: "float", nullable: false),
                    Silicates = table.Column<double>(type: "float", nullable: false),
                    SulphurDioxide = table.Column<double>(type: "float", nullable: false),
                    Nitrogen = table.Column<double>(type: "float", nullable: false),
                    Neon = table.Column<double>(type: "float", nullable: false),
                    Iron = table.Column<double>(type: "float", nullable: false),
                    Argon = table.Column<double>(type: "float", nullable: false),
                    Ammonia = table.Column<double>(type: "float", nullable: false),
                    Methane = table.Column<double>(type: "float", nullable: false),
                    Water = table.Column<double>(type: "float", nullable: false),
                    Oxygen = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtmosphereCompositions", x => x.BodyId64);
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
                    BodyId64 = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Antimony = table.Column<double>(type: "float", nullable: false),
                    Arsenic = table.Column<double>(type: "float", nullable: false),
                    Carbon = table.Column<double>(type: "float", nullable: false),
                    Iron = table.Column<double>(type: "float", nullable: false),
                    Nickel = table.Column<double>(type: "float", nullable: false),
                    Niobium = table.Column<double>(type: "float", nullable: false),
                    Phosphorus = table.Column<double>(type: "float", nullable: false),
                    Sulphur = table.Column<double>(type: "float", nullable: false),
                    Tin = table.Column<double>(type: "float", nullable: false),
                    Zinc = table.Column<double>(type: "float", nullable: false),
                    Zirconium = table.Column<double>(type: "float", nullable: false),
                    Cadmium = table.Column<double>(type: "float", nullable: false),
                    Manganese = table.Column<double>(type: "float", nullable: false),
                    Mercury = table.Column<double>(type: "float", nullable: false),
                    Tellurium = table.Column<double>(type: "float", nullable: false),
                    Vanadium = table.Column<double>(type: "float", nullable: false),
                    Chromium = table.Column<double>(type: "float", nullable: false),
                    Germanium = table.Column<double>(type: "float", nullable: false),
                    Molybdenum = table.Column<double>(type: "float", nullable: false),
                    Ruthenium = table.Column<double>(type: "float", nullable: false),
                    Yttrium = table.Column<double>(type: "float", nullable: false),
                    Selenium = table.Column<double>(type: "float", nullable: false),
                    Technetium = table.Column<double>(type: "float", nullable: false),
                    Tungsten = table.Column<double>(type: "float", nullable: false),
                    Polonium = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.BodyId64);
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
                    BodyId64 = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Local = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => new { x.BodyId64, x.Local });
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
                    BodyId64 = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    DetectionsHuman = table.Column<long>(name: "Detections_Human", type: "bigint", nullable: false),
                    DetectionsBiological = table.Column<long>(name: "Detections_Biological", type: "bigint", nullable: false),
                    DetectionsGeological = table.Column<long>(name: "Detections_Geological", type: "bigint", nullable: false),
                    DetectionsOther = table.Column<long>(name: "Detections_Other", type: "bigint", nullable: false),
                    DetectionsThargoid = table.Column<long>(name: "Detections_Thargoid", type: "bigint", nullable: false),
                    DetectionsGuardian = table.Column<long>(name: "Detections_Guardian", type: "bigint", nullable: false),
                    DetectionsPainite = table.Column<long>(name: "Detections_Painite", type: "bigint", nullable: false),
                    DetectionsPlatinum = table.Column<long>(name: "Detections_Platinum", type: "bigint", nullable: false),
                    DetectionsRhodplumsite = table.Column<long>(name: "Detections_Rhodplumsite", type: "bigint", nullable: false),
                    DetectionsSerendibite = table.Column<long>(name: "Detections_Serendibite", type: "bigint", nullable: false),
                    DetectionsAlexandrite = table.Column<long>(name: "Detections_Alexandrite", type: "bigint", nullable: false),
                    DetectionsBenitoite = table.Column<long>(name: "Detections_Benitoite", type: "bigint", nullable: false),
                    DetectionsMonazite = table.Column<long>(name: "Detections_Monazite", type: "bigint", nullable: false),
                    DetectionsMusgravite = table.Column<long>(name: "Detections_Musgravite", type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signals", x => x.BodyId64);
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
                    BodyId64 = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Ice = table.Column<double>(type: "float", nullable: false),
                    Metal = table.Column<double>(type: "float", nullable: false),
                    Rock = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolidCompositions", x => x.BodyId64);
                    table.ForeignKey(
                        name: "FK_SolidCompositions_Bodies_BodyId64",
                        column: x => x.BodyId64,
                        principalTable: "Bodies",
                        principalColumn: "Id64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Genera",
                columns: table => new
                {
                    SignalsBodyId64 = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genera", x => new { x.SignalsBodyId64, x.Name });
                    table.ForeignKey(
                        name: "FK_Genera_Signals_SignalsBodyId64",
                        column: x => x.SignalsBodyId64,
                        principalTable: "Signals",
                        principalColumn: "BodyId64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Biology_Genus_Species",
                table: "Biology",
                columns: new[] { "Genus", "Species" });

            migrationBuilder.CreateIndex(
                name: "IX_Bodies_IsLandable",
                table: "Bodies",
                column: "IsLandable",
                filter: "[Type] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Name", "Type", "SubType" });

            migrationBuilder.CreateIndex(
                name: "IX_Bodies_Name",
                table: "Bodies",
                column: "Name")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_Bodies_SystemId64_BodyId",
                table: "Bodies",
                columns: new[] { "SystemId64", "BodyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bodies_Type_IsLandable",
                table: "Bodies",
                columns: new[] { "Type", "IsLandable" },
                filter: "[Type] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Name", "SubType" });

            migrationBuilder.CreateIndex(
                name: "IX_Genera_Name_SignalsBodyId64",
                table: "Genera",
                columns: new[] { "Name", "SignalsBodyId64" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Systems_Name",
                table: "Systems",
                column: "Name")
                .Annotation("SqlServer:Clustered", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtmosphereCompositions");

            migrationBuilder.DropTable(
                name: "Biology");

            migrationBuilder.DropTable(
                name: "Genera");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropTable(
                name: "SolidCompositions");

            migrationBuilder.DropTable(
                name: "Signals");

            migrationBuilder.DropTable(
                name: "Bodies");

            migrationBuilder.DropTable(
                name: "Systems");
        }
    }
}
