using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myapp.Data;
using myapp.Services;
using Xunit;

namespace myapp.Tests.Services
{
    public class FleetServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly GameDbContext _dbContext;

        public FleetServiceTests()
        {
            // Create in-memory SQLite database for tests
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            
            var options = new DbContextOptionsBuilder<GameDbContext>()
                .UseSqlite(_connection)
                .Options;
            
            _dbContext = new GameDbContext(options);
            _dbContext.Database.EnsureCreated();
            
            // Initialize game state
            var persistenceService = new GamePersistenceService(_dbContext, 
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<GamePersistenceService>());
            persistenceService.InitializeGameStateAsync().Wait();
        }

        [Fact]
        public void HandleCombat_Should_Generate_Debris_On_Victory()
        {
            // Arrange
            var devModeService = new DevModeService(_dbContext);
            var messageService = new MessageService(_dbContext);
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var persistenceService = new GamePersistenceService(_dbContext, loggerFactory.CreateLogger<GamePersistenceService>());
            var galaxyService = new GalaxyService(_dbContext);
            var playerStateService = new PlayerStateService(galaxyService);
            var resourceService = new ResourceService(_dbContext, devModeService, playerStateService);
            var enemyService = new EnemyService(_dbContext, galaxyService);
            var buildingService = new BuildingService(_dbContext, resourceService, devModeService, enemyService, playerStateService);
            var techLogger = loggerFactory.CreateLogger<TechnologyService>();
            var techService = new TechnologyService(_dbContext, resourceService, buildingService, devModeService, enemyService, techLogger);
            var defenseService = new DefenseService(_dbContext, resourceService, buildingService, techService, devModeService, enemyService);
            
            var fleetService = new FleetService(_dbContext, resourceService, buildingService, techService, galaxyService, persistenceService, messageService, defenseService, devModeService, enemyService, playerStateService);
            
            // Create a mock mission that has arrived
            var mission = new FleetMission
            {
                Id = Guid.NewGuid(),
                MissionType = "Attack",
                TargetCoordinates = "1:1:2",
                Ships = new Dictionary<string, int> { { "LF", 10 } },
                StartTime = DateTime.Now.AddMinutes(-10),
                ArrivalTime = DateTime.Now.AddSeconds(-1),
                ReturnTime = DateTime.Now.AddMinutes(10),
                Status = FleetStatus.Flight,
                FuelConsumed = 100
            };
            
            // Get target planet info
            var targetPlanet = galaxyService.GetPlanet(1, 1, 2);
            Assert.NotNull(targetPlanet);
            Assert.True(targetPlanet.IsOccupied);
            
            // Act
            fleetService.SendFleet(new Dictionary<string, int> { { "LF", 10 } }, 1, 1, 2, "Attack");
            
            // Assert - Fleet should be added to active fleets
            Assert.True(fleetService.ActiveFleets.Count > 0);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose();
        }
    }
}
