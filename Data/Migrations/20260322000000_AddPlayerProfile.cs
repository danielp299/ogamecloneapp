using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myapp.Data.Migrations
{
    [DbContext(typeof(GameDbContext))]
    [Migration("20260322000000_AddPlayerProfile")]
    public partial class AddPlayerProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerName = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentSkin = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProfiles", x => x.Id);
                });

            // Fix planet image paths stored before the skin system was introduced.
            // Old format: "assets/planets/planet_home.jpg"
            // New format: "planets/planet_home.jpg"  (relative to skin folder)
            migrationBuilder.Sql(@"
UPDATE PlayerPlanets
SET Image = SUBSTR(Image, 8)
WHERE Image LIKE 'assets/%'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PlayerProfiles");
        }
    }
}
