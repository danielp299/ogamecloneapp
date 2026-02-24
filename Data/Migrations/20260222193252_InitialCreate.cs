using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuildingQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BuildingType = table.Column<string>(type: "TEXT", nullable: false),
                    TargetLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsProcessing = table.Column<bool>(type: "INTEGER", nullable: false),
                    QueuePosition = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BuildingType = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Galaxy = table.Column<int>(type: "INTEGER", nullable: false),
                    System = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DefenseQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefenseType = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityCompleted = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsProcessing = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefenseQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Defenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefenseType = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Galaxy = table.Column<int>(type: "INTEGER", nullable: false),
                    System = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Defenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Enemies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Galaxy = table.Column<int>(type: "INTEGER", nullable: false),
                    System = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpireId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsHomeworld = table.Column<bool>(type: "INTEGER", nullable: false),
                    Metal = table.Column<long>(type: "INTEGER", nullable: false),
                    Crystal = table.Column<long>(type: "INTEGER", nullable: false),
                    Deuterium = table.Column<long>(type: "INTEGER", nullable: false),
                    Energy = table.Column<long>(type: "INTEGER", nullable: false),
                    LastResourceUpdate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActivity = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsBot = table.Column<bool>(type: "INTEGER", nullable: false),
                    ColonyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BuildingsJson = table.Column<string>(type: "TEXT", nullable: false),
                    TechnologiesJson = table.Column<string>(type: "TEXT", nullable: false),
                    DefensesJson = table.Column<string>(type: "TEXT", nullable: false),
                    ShipsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ExploredGalaxiesJson = table.Column<string>(type: "TEXT", nullable: false),
                    KnownEnemyCoordinatesJson = table.Column<string>(type: "TEXT", nullable: false),
                    SpiedEnemyPowerJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enemies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FleetMissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MissionType = table.Column<string>(type: "TEXT", nullable: false),
                    TargetGalaxy = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetPosition = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReturnTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    FuelConsumed = table.Column<long>(type: "INTEGER", nullable: false),
                    CargoMetal = table.Column<long>(type: "INTEGER", nullable: false),
                    CargoCrystal = table.Column<long>(type: "INTEGER", nullable: false),
                    CargoDeuterium = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetMissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Metal = table.Column<double>(type: "REAL", nullable: false),
                    Crystal = table.Column<double>(type: "REAL", nullable: false),
                    Deuterium = table.Column<double>(type: "REAL", nullable: false),
                    DarkMatter = table.Column<double>(type: "REAL", nullable: false),
                    Energy = table.Column<long>(type: "INTEGER", nullable: false),
                    LastResourceUpdate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DevModeEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSavedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HomeGalaxy = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    HomePosition = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanetStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Galaxy = table.Column<int>(type: "INTEGER", nullable: false),
                    System = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Metal = table.Column<double>(type: "REAL", nullable: false),
                    Crystal = table.Column<double>(type: "REAL", nullable: false),
                    Deuterium = table.Column<double>(type: "REAL", nullable: false),
                    Energy = table.Column<long>(type: "INTEGER", nullable: false),
                    LastResourceUpdate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanetStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerPlanets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Galaxy = table.Column<int>(type: "INTEGER", nullable: false),
                    System = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Image = table.Column<string>(type: "TEXT", nullable: false),
                    IsHomeworld = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPlanets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TechnologyType = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsProcessing = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShipType = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Galaxy = table.Column<int>(type: "INTEGER", nullable: false),
                    System = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShipyardQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShipType = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityCompleted = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsProcessing = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipyardQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technologies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TechnologyType = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technologies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FleetMissionShips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FleetMissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShipType = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetMissionShips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FleetMissionShips_FleetMissions_FleetMissionId",
                        column: x => x.FleetMissionId,
                        principalTable: "FleetMissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_BuildingType_Galaxy_System_Position",
                table: "Buildings",
                columns: new[] { "BuildingType", "Galaxy", "System", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Defenses_DefenseType_Galaxy_System_Position",
                table: "Defenses",
                columns: new[] { "DefenseType", "Galaxy", "System", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enemies_Galaxy_System_Position",
                table: "Enemies",
                columns: new[] { "Galaxy", "System", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FleetMissionShips_FleetMissionId",
                table: "FleetMissionShips",
                column: "FleetMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanetStates_Galaxy_System_Position",
                table: "PlanetStates",
                columns: new[] { "Galaxy", "System", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPlanets_Galaxy_System_Position",
                table: "PlayerPlanets",
                columns: new[] { "Galaxy", "System", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ships_ShipType_Galaxy_System_Position",
                table: "Ships",
                columns: new[] { "ShipType", "Galaxy", "System", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_TechnologyType",
                table: "Technologies",
                column: "TechnologyType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildingQueue");

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "DefenseQueue");

            migrationBuilder.DropTable(
                name: "Defenses");

            migrationBuilder.DropTable(
                name: "Enemies");

            migrationBuilder.DropTable(
                name: "FleetMissionShips");

            migrationBuilder.DropTable(
                name: "GameState");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "PlanetStates");

            migrationBuilder.DropTable(
                name: "PlayerPlanets");

            migrationBuilder.DropTable(
                name: "ResearchQueue");

            migrationBuilder.DropTable(
                name: "Ships");

            migrationBuilder.DropTable(
                name: "ShipyardQueue");

            migrationBuilder.DropTable(
                name: "Technologies");

            migrationBuilder.DropTable(
                name: "FleetMissions");
        }
    }
}
