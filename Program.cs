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

// Register game initialization service
builder.Services.AddSingleton<GameInitializationService>();

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

// Initialize game on startup
// Step 1: First ensure database is created (before any service tries to access it)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("Database migrated/verified");
}

// Step 2: Now initialize all services (they can safely access the database)
using (var scope = app.Services.CreateScope())
{
    var initService = scope.ServiceProvider.GetRequiredService<GameInitializationService>();
    var galaxyService = scope.ServiceProvider.GetRequiredService<GalaxyService>();
    var enemyService = scope.ServiceProvider.GetRequiredService<EnemyService>();
    var resourceService = scope.ServiceProvider.GetRequiredService<ResourceService>();
    var buildingService = scope.ServiceProvider.GetRequiredService<BuildingService>();
    var technologyService = scope.ServiceProvider.GetRequiredService<TechnologyService>();
    var fleetService = scope.ServiceProvider.GetRequiredService<FleetService>();
    var defenseService = scope.ServiceProvider.GetRequiredService<DefenseService>();
    var playerStateService = scope.ServiceProvider.GetRequiredService<PlayerStateService>();

    await initService.InitializeGameAsync(
        galaxyService,
        enemyService,
        resourceService,
        buildingService,
        technologyService,
        fleetService,
        defenseService,
        playerStateService
    );
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
