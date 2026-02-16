//csharp
using Bunit;
using Bunit.TestDoubles; // Add this for FakeNavigationManager
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using myapp.Components.Pages;
using myapp.Data;
using myapp.Services;
using Xunit;
using System.Linq;

namespace myapp.Tests.Components.Pages
{
    public class ConstellationPageTests : TestContext
    {
        private GameDbContext CreateDbContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<GameDbContext>()
                .UseSqlite(connection)
                .Options;
            var dbContext = new GameDbContext(options);
            dbContext.Database.EnsureCreated();
            return dbContext;
        }

        [Fact]
        public void ConstellationPage_Render_Should_Render_Page() {
            var dbContext = CreateDbContext();
            Services.AddSingleton(dbContext);
            Services.AddSingleton<GalaxyService>();
            RenderComponent<ConstellationPage>();
        }

        [Fact]
        public void Clicking_Attack_Navigates_To_FleetPage_With_Correct_Params()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var galaxyService = new GalaxyService(dbContext);
            // Force a planet to be an enemy at 1:1:5
            var system = galaxyService.GetSystem(1, 1);
            var targetPlanet = system.FirstOrDefault(p => p.Position == 5);
            if (targetPlanet != null)
            {
                targetPlanet.IsOccupied = true;
                targetPlanet.IsMyPlanet = false;
                targetPlanet.PlayerName = "Enemy";
            }

            Services.AddSingleton(dbContext);
            Services.AddSingleton(galaxyService);
             
            var cut = RenderComponent<ConstellationPage>();

            // Act
            // Find the attack button for position 5. 
            // The row should contain "Enemy".
            var rows = cut.FindAll("tr.planet-row");
            var targetRow = rows.FirstOrDefault(r => r.TextContent.Contains("Enemy"));
            
            Assert.NotNull(targetRow);
            
            var attackButton = targetRow.QuerySelector("button"); // The first button is Attack in the template
            Assert.NotNull(attackButton);
            Assert.Contains("Attack", attackButton.TextContent);

            attackButton.Click();

            // Assert
            var navMan = Services.GetRequiredService<FakeNavigationManager>();
            // Expected URL: /fleet?galaxy=1&system=1&position=5&mission=Attack
            // Note: FakeNavigationManager.Uri is absolute.
            Assert.Contains("/fleet", navMan.Uri);
            Assert.Contains("galaxy=1", navMan.Uri);
            Assert.Contains("system=1", navMan.Uri);
            Assert.Contains("position=5", navMan.Uri);
            Assert.Contains("mission=Attack", navMan.Uri);
        }
    }
}