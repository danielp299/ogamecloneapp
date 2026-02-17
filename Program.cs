using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Services;
using myapp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add SQLite database as Singleton (required for singleton services)
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "game.db");
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"), ServiceLifetime.Singleton);

// Register persistence service
builder.Services.AddSingleton<GamePersistenceService>();

// Register game services
builder.Services.AddSingleton<PlayerStateService>();
builder.Services.AddSingleton<ResourceService>();
builder.Services.AddSingleton<BuildingService>();
builder.Services.AddSingleton<TechnologyService>();
builder.Services.AddSingleton<RequirementService>();
builder.Services.AddSingleton<FleetService>();
builder.Services.AddSingleton<DefenseService>();
builder.Services.AddSingleton<GalaxyService>();
builder.Services.AddSingleton<MessageService>();
builder.Services.AddSingleton<DevModeService>();
builder.Services.AddSingleton<EnemyService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var persistenceService = scope.ServiceProvider.GetRequiredService<GamePersistenceService>();
    await persistenceService.EnsureDatabaseCreatedAsync();
    await persistenceService.InitializeGameStateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
