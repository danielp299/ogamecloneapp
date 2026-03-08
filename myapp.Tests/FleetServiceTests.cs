using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<GameDbContext>()
                .UseSqlite(_connection)
                .Options;

            _dbContext = new GameDbContext(options);
            _dbContext.Database.EnsureCreated();
        }

        [Fact]
        public async Task HandleCombat_Should_Generate_Debris_On_Victory()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var persistenceService = new GamePersistenceService(_dbContext, loggerFactory.CreateLogger<GamePersistenceService>());
            await persistenceService.InitializeGameStateAsync();

            var devModeService = new DevModeService(_dbContext);
            var messageService = new MessageService(_dbContext);
            var galaxyService = new GalaxyService(_dbContext, persistenceService);
            var playerStateService = new PlayerStateService(galaxyService);
            var resourceService = new ResourceService(_dbContext, devModeService, playerStateService);
            var enemyService = new EnemyService(_dbContext, galaxyService);
            var buildingService = new BuildingService(_dbContext, resourceService, devModeService, enemyService, playerStateService);
            var techLogger = loggerFactory.CreateLogger<TechnologyService>();
            var techService = new TechnologyService(_dbContext, resourceService, buildingService, devModeService, enemyService, techLogger);
            var defenseService = new DefenseService(_dbContext, resourceService, buildingService, techService, devModeService, enemyService, playerStateService);

            var fleetService = new FleetService(_dbContext, resourceService, buildingService, techService, galaxyService, persistenceService, messageService, defenseService, devModeService, enemyService, playerStateService);

            await buildingService.InitializeAsync();
            await techService.InitializeAsync();
            await defenseService.InitializeAsync();
            await fleetService.InitializeAsync();
            await messageService.InitializeAsync();

            var targetPlanet = galaxyService.GetPlanet(1, 1, 2);
            Assert.NotNull(targetPlanet);
            Assert.True(targetPlanet.IsOccupied);

            // Act
            await fleetService.SendFleet(new Dictionary<string, int> { { "LF", 10 } }, 1, 1, 2, "Attack");
            await Task.Delay(300);

            // Assert
            var combatMessage = messageService.Messages.FirstOrDefault(m => m.Type == "Combat" && m.Subject.Contains("Combat Report"));
            Assert.NotNull(combatMessage);
            Assert.Contains("Your Attack Fleet", combatMessage.Body);
            Assert.Contains("Your Losses", combatMessage.Body);
            Assert.Contains("Debris Field", combatMessage.Body);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose();
        }
    }
}
