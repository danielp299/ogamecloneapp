using Bunit;
using myapp.Components.Pages;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using System;
namespace myapp.Tests
{
    public class BuildingsPageTests : Bunit.TestContext
    {
        
        
        [Fact]
        public void BuildingsPage_Render_Default_Buildings()
        {
            // Arrange
            var cut = RenderComponent<BuildingsPage>();

            // Assert
            var buildingsGrid = cut.Find("#buildings-grid");
            Assert.True(buildingsGrid.Children.Length >= 1);
        }
        [Fact]
        public void BuildingsPage_Render_Buildings_Grid()
        {
            //Arrange
            var buildings = new List<BuildingsPage.BuildingDto>();
            for (int i = 0; i < 3; i++)
            {
                buildings.Add(new BuildingsPage.BuildingDto { Title = $"Building {i}" });
            }
            var cut = RenderComponent<BuildingsPage>(ComponentParameter.CreateParameter("Buildings", buildings));

            //Assert
            var buildingCards = cut.FindAll(".building-card");
            Assert.Equal(3, buildingCards.Count);
        }

        [Fact]
        public void BuildingsPage_Render_Building_Level()
        {
            // Arrange
            var buildings = new List<BuildingsPage.BuildingDto>();
            for (int i = 1; i <= 3; i++)
            {
                buildings.Add(new BuildingsPage.BuildingDto { Title = $"Building {i}", Level = i });
            }
            var cut = RenderComponent<BuildingsPage>(ComponentParameter.CreateParameter("Buildings", buildings));

            // Act
            var buildingCards = cut.FindAll(".building-card");

            // Assert
            for (int i = 0; i < buildings.Count; i++)
            {
                Assert.Equal($"Level: {buildings[i].Level}", buildingCards[i].QuerySelector(".level").TextContent);
            }
        }
        [Fact]
        public void BuildingsPage_Render_Construction_Queue()
        {
            // Arrange
            var constructionQueue = new List<BuildingsPage.BuildingDto>();
            constructionQueue.Add(new BuildingsPage.BuildingDto
            {
                Title = "Building 1",
                ConstructionDuration = TimeSpan.FromSeconds(1)                
            });

            var cut = RenderComponent<BuildingsPage>(ComponentParameter.CreateParameter("ConstructionQueue", constructionQueue));

            // Assert
            var constructionQueueDiv = cut.Find("#construction-queue");
            Assert.NotNull(constructionQueueDiv);
        }

        [Fact] 
        public void BuildingsPage_Render_Should_Render_Construct_Button_For_Level_0()  
        {
            Guid buildingId = Guid.NewGuid();           
            var building = new BuildingsPage.BuildingDto { Level = 0, Id = buildingId, ConstructionDuration = TimeSpan.FromSeconds(2)};
            var cut = RenderComponent<BuildingsPage>(ComponentParameter.CreateParameter(nameof(BuildingsPage.Buildings), new List<BuildingsPage.BuildingDto>() { building }));

            AssertButtonIsRendered($"#construct-building-{building.Id}", cut);
        }

        [Fact]
        public void BuildingsPage_Dismantle_Hides_Buttons()
        {
            Guid buildingId = Guid.NewGuid();
            var building = new BuildingsPage.BuildingDto { Level = 1, Id = buildingId , ConstructionDuration = TimeSpan.FromSeconds(2)};
            var cut = RenderComponent<BuildingsPage>(ComponentParameter.CreateParameter(nameof(BuildingsPage.Buildings), new List<BuildingsPage.BuildingDto>() { building }));

            AssertButtonIsRendered($"#dismantle-building-{building.Id}", cut);            
            AssertButtonIsNotRendered($"#cancel-building-{building.Id}", cut);
        }

        [Fact]
        public void BuildingsPage_Render_Should_Render_Dismantle_And_Upgrade_Buttons_For_Level_1()
        {

            Guid buildingId = Guid.NewGuid();
            var building = new BuildingsPage.BuildingDto { Level = 1, Id = buildingId };
            var cut = RenderComponent<BuildingsPage>(ComponentParameter.CreateParameter(nameof(BuildingsPage.Buildings), new List<BuildingsPage.BuildingDto>() { building }));

            AssertButtonIsRendered($"#dismantle-building-{building.Id}", cut);
            AssertButtonIsRendered($"#upgrade-building-{building.Id}", cut);
        }

        

        private void AssertButtonIsRendered(string id, IRenderedComponent<BuildingsPage> cut)
        {
            var button = cut.Find(id);
            Assert.NotNull(button);
        }
        private void AssertButtonIsNotRendered(string id, IRenderedComponent<BuildingsPage> cut)
        {            
            Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(id));
        }


    }
}