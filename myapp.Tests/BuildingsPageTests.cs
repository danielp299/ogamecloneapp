//csharp
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
            RenderComponent<BuildingsPage>();
        }
    }
}