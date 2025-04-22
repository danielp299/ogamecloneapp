//csharp
using Bunit;
using myapp.Components.Layout;
using Xunit;

namespace myapp.Tests.Components.Layout
{
    public class NavMenuTests : Bunit.TestContext
    {
        [Fact]
        public void NavMenu_Render_Should_Render_Page()
        {
            RenderComponent<NavMenu>();
        }
    }
}