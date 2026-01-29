using Bunit;
using Xunit;
using myapp.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using myapp.Services;

namespace myapp.Tests
{
    public class PageTests : TestContext
    {
        public PageTests()
        {
            // Register services
            Services.AddSingleton<ResourceService>();
            Services.AddSingleton<BuildingService>();
            Services.AddSingleton<TechnologyService>();
            Services.AddSingleton<FleetService>();
            Services.AddSingleton<DefenseService>();
            Services.AddSingleton<MessageService>();
            Services.AddSingleton<GalaxyService>();
            Services.AddSingleton<RequirementService>();
            Services.AddSingleton<DevModeService>();
        }

        [Fact]
        public void Home_Page_Loads()
        {
            var cut = RenderComponent<Home>();
            Assert.Contains("Command Center", cut.Markup);
        }

        [Fact]
        public void Buildings_Page_Loads()
        {
            var cut = RenderComponent<BuildingsPage>();
            Assert.Contains("Buildings", cut.Markup);
        }

        [Fact]
        public void Technologies_Page_Loads()
        {
            var cut = RenderComponent<TechnologyPage>();
            Assert.Contains("Technologies", cut.Markup);
        }

        [Fact]
        public void Factory_Page_Loads()
        {
            var cut = RenderComponent<FactoryPage>();
            Assert.Contains("Shipyard", cut.Markup);
        }

        [Fact]
        public void Fleet_Page_Loads()
        {
            var cut = RenderComponent<FleetPage>();
            Assert.Contains("Fleet Command", cut.Markup);
        }

        [Fact]
        public void Defense_Page_Loads()
        {
            var cut = RenderComponent<DefensePage>();
            Assert.Contains("Defense", cut.Markup);
        }
        
        [Fact]
        public void Messages_Page_Loads()
        {
            var cut = RenderComponent<Messages>();
            Assert.Contains("Messages", cut.Markup);
        }
        
        [Fact]
        public void Constellation_Page_Loads()
        {
            var cut = RenderComponent<ConstellationPage>();
            // The table headers are constant, so we can check for one of them
            Assert.Contains("Planet Name", cut.Markup);
        }
    }
}
