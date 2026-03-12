using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myapp.Data.Migrations
{
    [DbContext(typeof(GameDbContext))]
    [Migration("20260312123000_AddPlanetCoordinatesToQueues")]
    public partial class AddPlanetCoordinatesToQueues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Galaxy",
                table: "BuildingQueue",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "BuildingQueue",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "System",
                table: "BuildingQueue",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Galaxy",
                table: "ShipyardQueue",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "ShipyardQueue",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "System",
                table: "ShipyardQueue",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Galaxy",
                table: "DefenseQueue",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "DefenseQueue",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "System",
                table: "DefenseQueue",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
UPDATE BuildingQueue
SET Galaxy = COALESCE((SELECT HomeGalaxy FROM GameState LIMIT 1), 0),
    System = COALESCE((SELECT HomeSystem FROM GameState LIMIT 1), 0),
    Position = COALESCE((SELECT HomePosition FROM GameState LIMIT 1), 0)");

            migrationBuilder.Sql(@"
UPDATE ShipyardQueue
SET Galaxy = COALESCE((SELECT HomeGalaxy FROM GameState LIMIT 1), 0),
    System = COALESCE((SELECT HomeSystem FROM GameState LIMIT 1), 0),
    Position = COALESCE((SELECT HomePosition FROM GameState LIMIT 1), 0)");

            migrationBuilder.Sql(@"
UPDATE DefenseQueue
SET Galaxy = COALESCE((SELECT HomeGalaxy FROM GameState LIMIT 1), 0),
    System = COALESCE((SELECT HomeSystem FROM GameState LIMIT 1), 0),
    Position = COALESCE((SELECT HomePosition FROM GameState LIMIT 1), 0)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Galaxy",
                table: "BuildingQueue");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "BuildingQueue");

            migrationBuilder.DropColumn(
                name: "System",
                table: "BuildingQueue");

            migrationBuilder.DropColumn(
                name: "Galaxy",
                table: "ShipyardQueue");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "ShipyardQueue");

            migrationBuilder.DropColumn(
                name: "System",
                table: "ShipyardQueue");

            migrationBuilder.DropColumn(
                name: "Galaxy",
                table: "DefenseQueue");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "DefenseQueue");

            migrationBuilder.DropColumn(
                name: "System",
                table: "DefenseQueue");
        }
    }
}
