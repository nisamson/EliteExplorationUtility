using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EEU.Migrations
{
    /// <inheritdoc />
    public partial class CodexRaw : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodexEntries",
                columns: table => new
                {
                    ValueHash = table.Column<byte[]>(type: "VARBINARY(64)", nullable: false, computedColumnSql: "CONVERT(varbinary(64), HASHBYTES('SHA2_512', Value))", stored: true),
                    Value = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodexEntries", x => x.ValueHash)
                        .Annotation("SqlServer:Clustered", false);
                    table.CheckConstraint("CK_Value_ValidJson", "ISJSON([Value]) > 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodexEntries");
        }
    }
}
