using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

public class GalaxyPlanet
{
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }
    public string Name { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string Alliance { get; set; } = "";
    public bool IsOccupied { get; set; }
    public bool IsMyPlanet { get; set; }
    public bool IsHomeworld { get; set; }
    public bool HasDebris { get; set; }
    public long DebrisMetal { get; set; }
    public long DebrisCrystal { get; set; }
    public string Image { get; set; } = "assets/planets/planet_placeholder.png"; // Default
}

public class GalaxyService
{
    private readonly GameDbContext _dbContext;
    private readonly GamePersistenceService _persistenceService;
    
    // Key: "galaxy:system" (e.g., "1:1") -> List of 15 planets
    private Dictionary<string, List<GalaxyPlanet>> _universe = new();
    private Random _random = new Random();

    // List of planets owned by the player
    public List<GalaxyPlanet> PlayerPlanets { get; private set; } = new();

    // Define the player's home planet for reference
    public int HomeGalaxy { get; private set; }
    public int HomeSystem { get; private set; }
    public int HomePosition { get; private set; }

    // Universe limits: 10 galaxies, 10 systems per galaxy
    public const int MaxGalaxies = 10;
    public const int MaxSystemsPerGalaxy = 10;

    public event Action? OnChange;
    private void NotifyStateChanged() => OnChange?.Invoke();

    private bool _isInitialized = false;

    public GalaxyService(GameDbContext dbContext, GamePersistenceService persistenceService)
    {
        _dbContext = dbContext;
        _persistenceService = persistenceService;
        // NOTA: La inicialización se hace de forma lazy via Initialize()
    }

    public void SetHomeCoordinates(int galaxy, int system, int position)
    {
        HomeGalaxy = galaxy;
        HomeSystem = system;
        HomePosition = position;
    }

    public void Initialize()
    {
        if (_isInitialized) return;

        // Ensure home system exists and has the player
        var homeSystem = GetSystem(HomeGalaxy, HomeSystem);
        
        // Find and register home planet
        var homePlanet = homeSystem.FirstOrDefault(p => p.Position == HomePosition);
        if (homePlanet != null)
        {
            PlayerPlanets.Add(homePlanet);
        }

        _isInitialized = true;
        Console.WriteLine($"GalaxyService initialized with home: {HomeGalaxy}:{HomeSystem}:{HomePosition}");
    }

    public void RefreshSystems()
    {
        // Clear universe cache to reload with enemy data
        _universe.Clear();
        
        // Re-initialize home system
        var homeSystem = GetSystem(HomeGalaxy, HomeSystem);
        var homePlanet = homeSystem.FirstOrDefault(p => p.Position == HomePosition);
        if (homePlanet != null && !PlayerPlanets.Contains(homePlanet))
        {
            PlayerPlanets.Add(homePlanet);
        }
        
        NotifyStateChanged();
    }

    public void ResetState()
    {
        _universe.Clear();
        PlayerPlanets.Clear();
        HomeGalaxy = 0;
        HomeSystem = 0;
        HomePosition = 0;
        _isInitialized = false;
    }
    
    public async Task SaveHomeCoordinatesAsync()
    {
        try
        {
            var gameState = await _dbContext.GameState.FirstOrDefaultAsync();
            if (gameState != null)
            {
                gameState.HomeGalaxy = HomeGalaxy;
                gameState.HomeSystem = HomeSystem;
                gameState.HomePosition = HomePosition;
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving home coordinates: {ex.Message}");
        }
    }

    public void RegisterPlanet(GalaxyPlanet planet)
    {
        if (!PlayerPlanets.Contains(planet))
        {
            PlayerPlanets.Add(planet);
        }
    }

    public void UnregisterPlanet(GalaxyPlanet planet)
    {
        if (PlayerPlanets.Contains(planet))
        {
            PlayerPlanets.Remove(planet);
        }
    }

    public List<GalaxyPlanet> GetSystem(int galaxy, int system)
    {
        // Validate limits
        if (galaxy < 1 || galaxy > MaxGalaxies)
            throw new ArgumentOutOfRangeException(nameof(galaxy), $"Galaxy must be between 1 and {MaxGalaxies}");
        if (system < 1 || system > MaxSystemsPerGalaxy)
            throw new ArgumentOutOfRangeException(nameof(system), $"System must be between 1 and {MaxSystemsPerGalaxy}");

        string key = $"{galaxy}:{system}";
        
        if (!_universe.ContainsKey(key))
        {
            _universe[key] = GenerateSystem(galaxy, system);
        }

        return _universe[key];
    }

    private List<GalaxyPlanet> GenerateSystem(int galaxy, int system)
    {
        var planets = new List<GalaxyPlanet>();

        // Fetch enemies for this system from DB to ensure persistence
        var systemEnemies = _dbContext.Enemies
            .Where(e => e.Galaxy == galaxy && e.System == system)
            .ToList();

        for (int i = 1; i <= 15; i++)
        {
            var planet = new GalaxyPlanet
            {
                Galaxy = galaxy,
                System = system,
                Position = i,
                IsOccupied = false
            };

            // 1. Check if it's the Player's Home
            if (galaxy == HomeGalaxy && system == HomeSystem && i == HomePosition)
            {
                planet.IsOccupied = true;
                planet.IsMyPlanet = true;
                planet.IsHomeworld = true;
                planet.Name = "Homeworld";
                planet.PlayerName = "Commander";
                planet.Image = "assets/planets/planet_home.jpg";
            }
            // 2. Check if it's a Player's Colony
            else if (PlayerPlanets.Any(p => p.Galaxy == galaxy && p.System == system && p.Position == i))
            {
                var existingColony = PlayerPlanets.First(p => p.Galaxy == galaxy && p.System == system && p.Position == i);
                planet.IsOccupied = true;
                planet.IsMyPlanet = true;
                planet.Name = existingColony.Name;
                planet.PlayerName = "Commander";
                planet.Image = existingColony.Image;
            }
            // 3. Check if it's an Enemy
            else
            {
                var enemy = systemEnemies.FirstOrDefault(e => e.Position == i);
                if (enemy != null)
                {
                    planet.IsOccupied = true;
                    planet.IsMyPlanet = false;
                    planet.Name = enemy.Name;
                    planet.PlayerName = enemy.Name;
                    planet.Alliance = enemy.IsBot ? "[BOTS]" : "";
                    planet.Image = GetRandomPlanetImage(i);
                }
            }

            planets.Add(planet);
        }

        return planets;
    }

    public void AbandonPlanet(GalaxyPlanet planet)
    {
        if (planet.IsHomeworld) return; // Cannot abandon homeworld
        
        // Remove from player list
        UnregisterPlanet(planet);
        
        // Reset planet state
        planet.IsOccupied = false;
        planet.IsMyPlanet = false;
        planet.Name = "";
        planet.PlayerName = "";
        planet.Alliance = "";
        planet.Image = GetRandomPlanetImage(planet.Position); // Reset image to default random type
        
        // Note: In a full implementation, we should also cancel active queues 
        // and handle fleets associated with this planet.
    }

    private string GetRandomPlanetImage(int position)
    {
        // Simple logic: hotter planets closer to star (1-3), colder further away (13-15)
        if (position <= 3) return "assets/planets/planet_hot.jpg";
        if (position >= 13) return "assets/planets/planet_ice.jpg";

        // Random variation for habitable zone
        string[] types = { "assets/planets/planet_gas.jpg", "assets/planets/planet_water.png", "assets/planets/planet_desert.png", "assets/planets/planet_forest.png" };
        return types[_random.Next(types.Length)];
    }

    public GalaxyPlanet? GetPlanet(int galaxy, int system, int position)
    {
        var systemPlanets = GetSystem(galaxy, system);
        return systemPlanets.FirstOrDefault(p => p.Position == position);
    }
}
