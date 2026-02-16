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

    public GalaxyService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
        
        // Load or generate home coordinates
        LoadOrGenerateHomeCoordinates();

        // Ensure home system exists and has the player
        var homeSystem = GetSystem(HomeGalaxy, HomeSystem);
        
        // Find and register home planet
        var homePlanet = homeSystem.FirstOrDefault(p => p.Position == HomePosition);
        if (homePlanet != null)
        {
            PlayerPlanets.Add(homePlanet);
        }
    }
    
    private void LoadOrGenerateHomeCoordinates()
    {
        try
        {
            var gameState = _dbContext.GameState.FirstOrDefault();
            
            if (gameState != null && gameState.HomeGalaxy > 0 && gameState.HomeSystem > 0 && gameState.HomePosition > 0)
            {
                // Load existing coordinates from database
                HomeGalaxy = gameState.HomeGalaxy;
                HomeSystem = gameState.HomeSystem;
                HomePosition = gameState.HomePosition;
                Console.WriteLine($"Loaded player home location: {HomeGalaxy}:{HomeSystem}:{HomePosition}");
            }
            else
            {
                // Generate random coordinates for new game
                HomeGalaxy = _random.Next(1, MaxGalaxies + 1);
                HomeSystem = _random.Next(1, MaxSystemsPerGalaxy + 1);
                HomePosition = _random.Next(1, 16); // 1 to 15
                
                // Save to database
                if (gameState == null)
                {
                    gameState = new GameState { Id = 1 };
                    _dbContext.GameState.Add(gameState);
                }
                
                gameState.HomeGalaxy = HomeGalaxy;
                gameState.HomeSystem = HomeSystem;
                gameState.HomePosition = HomePosition;
                _dbContext.SaveChanges();
                
                Console.WriteLine($"Generated and saved new player home location: {HomeGalaxy}:{HomeSystem}:{HomePosition}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading home coordinates: {ex.Message}");
            // Fallback to random coordinates
            HomeGalaxy = _random.Next(1, MaxGalaxies + 1);
            HomeSystem = _random.Next(1, MaxSystemsPerGalaxy + 1);
            HomePosition = _random.Next(1, 16);
        }
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

        for (int i = 1; i <= 15; i++)
        {
            var planet = new GalaxyPlanet
            {
                Galaxy = galaxy,
                System = system,
                Position = i,
                IsOccupied = false
            };

            // Hardcode Player Home
            if (galaxy == HomeGalaxy && system == HomeSystem && i == HomePosition)
            {
                planet.IsOccupied = true;
                planet.IsMyPlanet = true;
                planet.IsHomeworld = true;
                planet.Name = "Homeworld";
                planet.PlayerName = "Commander";
                planet.Image = "assets/planets/planet_home.jpg";
            }
            // Randomly populate other slots
            else if (_random.NextDouble() < 0.3) // 30% chance of occupation
            {
                planet.IsOccupied = true;
                planet.IsMyPlanet = false;
                planet.Name = $"Planet {galaxy}:{system}:{i}";
                planet.PlayerName = $"Player_{_random.Next(1000, 9999)}";
                planet.Alliance = _random.NextDouble() < 0.2 ? "[BOTS]" : "";
                planet.Image = GetRandomPlanetImage(i);
                
                // Random debris
                if (_random.NextDouble() < 0.2)
                {
                    planet.HasDebris = true;
                    planet.DebrisMetal = _random.Next(1000, 50000);
                    planet.DebrisCrystal = _random.Next(1000, 50000);
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
