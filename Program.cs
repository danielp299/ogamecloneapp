using myapp.Services;
using myapp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddSingleton<ResourceService>();
builder.Services.AddSingleton<BuildingService>();
builder.Services.AddSingleton<TechnologyService>();
builder.Services.AddSingleton<FleetService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // app.UseHsts(); // Disable HSTS for now to avoid strict HTTPS requirements locally
}

// app.UseHttpsRedirection(); // Disable HTTPS redirection to allow plain HTTP on localhost


app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

