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
            // Use raw SQL with IF NOT EXISTS to handle cases where the table
            // was already created outside of migrations.
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""PlayerProfiles"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PlayerProfiles"" PRIMARY KEY,
    ""PlayerName"" TEXT NOT NULL,
    ""CurrentSkin"" TEXT NOT NULL
)");

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
