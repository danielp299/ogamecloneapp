//csharp
using Bunit;
using myapp.Components.Pages;
using Xunit;

namespace myapp.Tests.Components.Pages
{
    public class FactoryPageTests : Bunit.TestContext
    {
        [Fact]
        public void FactoryPage_Render_Should_Render_Page()
        {
            RenderComponent<FactoryPage>();
        }
    }
}