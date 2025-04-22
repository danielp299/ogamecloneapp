//csharp
using Bunit;
using myapp.Components.Pages;
using Xunit;

namespace myapp.Tests.Components.Pages
{
    public class DefensePageTests : Bunit.TestContext
    {
        [Fact]
        public void DefensePage_Render_Should_Render_Page()
        {
            RenderComponent<DefensePage>();
        }
    }
}