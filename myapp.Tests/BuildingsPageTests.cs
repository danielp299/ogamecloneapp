//csharp
using AngleSharp.Dom;
using Bunit;
using myapp.Components.Pages;
using Xunit;

namespace myapp.Tests.Components.Pages
{
    public class BuildingsPageTests : Bunit.TestContext
    {
       [Fact]
        public void BuildingsPage_Render_Should_Render_Page()
        {
            var cut = RenderComponent<BuildingsPage>();
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

       
        private string GetCancelId(BuildingsPage.BuildingDto building)
        {
            return $"#cancel-building-{building.Id}";
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