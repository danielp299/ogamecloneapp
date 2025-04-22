//csharp
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using myapp.Components.Pages;
using Xunit;

namespace myapp.Tests.Components.Pages
{
    public class ConstellationPageTests : Bunit.TestContext
    {
        [Fact]
        public void ConstellationPage_Render_Should_Render_Page() {
            RenderComponent<ConstellationPage>();
        }
    }
}