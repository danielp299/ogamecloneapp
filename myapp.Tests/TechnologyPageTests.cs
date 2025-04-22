//csharp
using Bunit;
using myapp.Components.Pages;
using Xunit;

namespace myapp.Tests.Components.Pages
{
    public class TechnologyPageTests : Bunit.TestContext
    {
        [Fact]
        public void TechnologyPage_Render_Should_Render_Page()
        {
            RenderComponent<TechnologyPage>();
        }
    }
}