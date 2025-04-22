//csharp
using Bunit;
using myapp.Components.Pages;
using Xunit;

namespace myapp.Tests.Components.Pages
{
    public class HomePageTests : Bunit.TestContext
    {        
        [Fact]
        public void HomePage_Render_Should_Render_Banner_QueueArea_PlanetArea() {
            RenderComponent<Home>();
        }
    }
}