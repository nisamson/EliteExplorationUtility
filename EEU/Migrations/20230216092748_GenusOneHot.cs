using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EEU.Migrations
{
    /// <inheritdoc />
    public partial class GenusOneHot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql(
                @"
                CREATE FUNCTION dbo.HasGenus(@bodyId DECIMAL(20), @genus NVARCHAR(100))
                RETURNS BIT
                WITH RETURNS NULL ON NULL INPUT
                AS BEGIN 
                    DECLARE @out BIT;
                    SET @out = IIF(EXISTS(SELECT * FROM Genera G WHERE G.SignalsBodyId64 = @bodyId AND G.Name = @genus), 1, 0);
                    RETURN @out;
                END
                ");
            
            migrationBuilder.Sql(@"
                CREATE VIEW OneHotGenera AS
                SELECT
                    B.Id64 AS BodyId64,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Aleoids_Genus_Name;' THEN 1 END) AS BIT) AS Aleoids,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Bacterial_Genus_Name;' THEN 1 END) AS BIT) AS Bacterial,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Cactoid_Genus_Name;' THEN 1 END) AS BIT) AS Cactoid,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Clypeus_Genus_Name;' THEN 1 END) AS BIT) AS Clypeus,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Conchas_Genus_Name;' THEN 1 END) AS BIT) AS Conchas,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Electricae_Genus_Name;' THEN 1 END) AS BIT) AS Electricae,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Fonticulus_Genus_Name;' THEN 1 END) AS BIT) AS Fonticulus,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Fumerolas_Genus_Name;' THEN 1 END) AS BIT) AS Fumerolas,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Fungoids_Genus_Name;' THEN 1 END) AS BIT) AS Fungoids,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Osseus_Genus_Name;' THEN 1 END) AS BIT) AS Osseus,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Recepta_Genus_Name;' THEN 1 END) AS BIT) AS Recepta,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Shrubs_Genus_Name;' THEN 1 END) AS BIT) AS Shrubs,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Stratum_Genus_Name;' THEN 1 END) AS BIT) AS Stratum,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Tubus_Genus_Name;' THEN 1 END) AS BIT) AS Tubus,
                    CAST(COUNT(CASE WHEN G.Name = '$Codex_Ent_Tussocks_Genus_Name;' THEN 1 END) AS BIT) AS Tussocks
                FROM dbo.Bodies B
                INNER JOIN dbo.Genera G ON G.SignalsBodyId64 = B.Id64
                GROUP BY B.Id64
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql(@"DROP VIEW OneHotGenera");
            migrationBuilder.Sql(@"DROP FUNCTION dbo.HasGenus");
        }
    }
}
