using Bunit;
using myapp.Components;
using myapp;
using Xunit;
using Bunit.JSInterop;

namespace myapp.Tests.Components
{
    public class AppTests : Bunit.TestContext
    {
        [Fact]
        public void App_Render_Should_Render_Page(){
            JSInterop.Setup<string>("Blazor._internal.PageTitle.getAndRemoveExistingTitle").SetResult("Test Title");
            RenderComponent<App>();
        }
    }
}