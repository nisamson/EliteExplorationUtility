using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EEU.Migrations
{
    /// <inheritdoc />
    public partial class SpeciesInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Species",
                columns: table => new
                {
                    Genus = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Species = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    ClonalRange = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Species", x => new { x.Genus, x.Species });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Species");
        }
    }
}
