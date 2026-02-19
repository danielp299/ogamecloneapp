using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

public class GamePersistenceService
{
    private readonly GameDbContext _dbContext;
    private readonly ILogger<GamePersistenceService> _logger;

    public GamePersistenceService(GameDbContext dbContext, ILogger<GamePersistenceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
        _logger.LogInformation("Database created/verified");
    }

    public async Task<GameState?> GetGameStateAsync()
    {
        return await _dbContext.GameState.FirstOrDefaultAsync();
    }

    public async Task<GameState> InitializeGameStateAsync()
    {
        var existing = await _dbContext.GameState.FirstOrDefaultAsync();
        if (existing != null) return existing;

        // Create initial game state
        var gameState = new GameState
        {
            Id = 1,
            Metal = 50000,
            Crystal = 50000,
            Deuterium = 50000,
            DarkMatter = 0,
            Energy = 0,
            DevModeEnabled = true,
            CreatedAt = DateTime.UtcNow,
            LastResourceUpdate = DateTime.UtcNow
        };

        _dbContext.GameState.Add(gameState);
        await _dbContext.SaveChangesAsync();

        // Initialize home planet state and buildings
        await InitializePlanetAsync(gameState.HomeGalaxy, gameState.HomeSystem, gameState.HomePosition);
        await AddOrUpdatePlayerPlanetAsync(gameState.HomeGalaxy, gameState.HomeSystem, gameState.HomePosition, "Homeworld", "assets/planets/planet_home.jpg", true);

        _logger.LogInformation("Game state initialized");
        return gameState;
    }

    public async Task InitializePlanetAsync(int g, int s, int p, bool resetIfExists = false)
    {
        if (resetIfExists)
        {
            bool hasData = await _dbContext.PlanetStates.AnyAsync(ps => ps.Galaxy == g && ps.System == s && ps.Position == p)
                || await _dbContext.Buildings.AnyAsync(b => b.Galaxy == g && b.System == s && b.Position == p)
                || await _dbContext.Ships.AnyAsync(sh => sh.Galaxy == g && sh.System == s && sh.Position == p)
                || await _dbContext.Defenses.AnyAsync(d => d.Galaxy == g && d.System == s && d.Position == p);

            if (hasData)
            {
                var existingPlanet = await _dbContext.PlanetStates.FirstOrDefaultAsync(ps => ps.Galaxy == g && ps.System == s && ps.Position == p);
                var buildings = _dbContext.Buildings.Where(b => b.Galaxy == g && b.System == s && b.Position == p);
                var ships = _dbContext.Ships.Where(sh => sh.Galaxy == g && sh.System == s && sh.Position == p);
                var defenses = _dbContext.Defenses.Where(d => d.Galaxy == g && d.System == s && d.Position == p);

                _dbContext.Buildings.RemoveRange(buildings);
                _dbContext.Ships.RemoveRange(ships);
                _dbContext.Defenses.RemoveRange(defenses);
                if (existingPlanet != null)
                {
                    _dbContext.PlanetStates.Remove(existingPlanet);
                }
                await _dbContext.SaveChangesAsync();
            }
        }

        // Add PlanetState
        if (!await _dbContext.PlanetStates.AnyAsync(ps => ps.Galaxy == g && ps.System == s && ps.Position == p))
        {
            _dbContext.PlanetStates.Add(new PlanetState
            {
                Galaxy = g,
                System = s,
                Position = p,
                Metal = 500,
                Crystal = 500,
                Deuterium = 0
            });
        }

        // Initialize buildings for this planet
        var buildingTypes = new[]
        {
            "Metal Mine", "Crystal Mine", "Deuterium Synthesizer", "Solar Plant",
            "Robotics Factory", "Fusion Reactor", "Alliance Depot", "Shipyard",
            "Metal Storage", "Crystal Storage", "Deuterium Tank", "Research Lab",
            "Terraformer", "Missile Silo", "Nanite Factory"
        };

        foreach (var type in buildingTypes)
        {
            if (!await _dbContext.Buildings.AnyAsync(b => b.BuildingType == type && b.Galaxy == g && b.System == s && b.Position == p))
            {
                _dbContext.Buildings.Add(new BuildingEntity
                {
                    BuildingType = type,
                    Level = 0,
                    Galaxy = g,
                    System = s,
                    Position = p
                });
            }
        }

        // Ships and Defenses are also per planet
        var shipTypes = new[] { "SC", "LC", "LF", "HF", "CR", "BS", "CS", "REC", "ESP", "DST", "RIP" };
        foreach (var type in shipTypes)
        {
            if (!await _dbContext.Ships.AnyAsync(sh => sh.ShipType == type && sh.Galaxy == g && sh.System == s && sh.Position == p))
            {
                _dbContext.Ships.Add(new ShipEntity
                {
                    ShipType = type,
                    Quantity = 0,
                    Galaxy = g,
                    System = s,
                    Position = p
                });
            }
        }

        var defenseTypes = new[] { "RL", "LL", "HL", "GC", "IC", "PT", "SSD", "LSD", "ABM", "IPM" };
        foreach (var type in defenseTypes)
        {
            if (!await _dbContext.Defenses.AnyAsync(d => d.DefenseType == type && d.Galaxy == g && d.System == s && d.Position == p))
            {
                _dbContext.Defenses.Add(new DefenseEntity
                {
                    DefenseType = type,
                    Quantity = 0,
                    Galaxy = g,
                    System = s,
                    Position = p
                });
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<PlayerPlanetEntity>> GetPlayerPlanetsAsync()
    {
        return await _dbContext.PlayerPlanets.ToListAsync();
    }

    public async Task AddOrUpdatePlayerPlanetAsync(int g, int s, int p, string name, string image, bool isHomeworld)
    {
        var planet = await _dbContext.PlayerPlanets
            .FirstOrDefaultAsync(pl => pl.Galaxy == g && pl.System == s && pl.Position == p);

        if (planet == null)
        {
            planet = new PlayerPlanetEntity
            {
                Galaxy = g,
                System = s,
                Position = p,
                Name = name,
                Image = image,
                IsHomeworld = isHomeworld
            };
            _dbContext.PlayerPlanets.Add(planet);
        }
        else
        {
            planet.Name = name;
            planet.Image = image;
            planet.IsHomeworld = isHomeworld;
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task InitializeBuildingsAsync()
    {
        // This method is now redundant but kept for compatibility if called elsewhere, 
        // though it should probably be removed or redirected.
    }

    private async Task InitializeTechnologiesAsync()
    {
        var techTypes = new[]
        {
            "Espionage Technology", "Computer Technology", "Weapons Technology",
            "Shielding Technology", "Armour Technology", "Energy Technology",
            "Hyperspace Technology", "Combustion Drive", "Impulse Drive",
            "Hyperspace Drive", "Laser Technology", "Ion Technology",
            "Plasma Technology", "Intergalactic Research Network", "Astrophysics",
            "Graviton Technology"
        };

        foreach (var type in techTypes)
        {
            _dbContext.Technologies.Add(new Data.Entities.TechnologyEntity
            {
                TechnologyType = type,
                Level = 0
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task InitializeShipsAsync()
    {
        var shipTypes = new[] { "SC", "LC", "LF", "HF", "CR", "BS", "CS", "REC", "ESP", "DST", "RIP" };

        foreach (var type in shipTypes)
        {
            _dbContext.Ships.Add(new Data.Entities.ShipEntity
            {
                ShipType = type,
                Quantity = 0
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task InitializeDefensesAsync()
    {
        var defenseTypes = new[] { "RL", "LL", "HL", "GC", "IC", "PT", "SSD", "LSD", "ABM", "IPM" };

        foreach (var type in defenseTypes)
        {
            _dbContext.Defenses.Add(new Data.Entities.DefenseEntity
            {
                DefenseType = type,
                Quantity = 0
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task ResetGameAsync()
    {
        // Clear all data
        _dbContext.FleetMissionShips.RemoveRange(_dbContext.FleetMissionShips);
        _dbContext.FleetMissions.RemoveRange(_dbContext.FleetMissions);
        _dbContext.Messages.RemoveRange(_dbContext.Messages);
        _dbContext.BuildingQueue.RemoveRange(_dbContext.BuildingQueue);
        _dbContext.ShipyardQueue.RemoveRange(_dbContext.ShipyardQueue);
        _dbContext.DefenseQueue.RemoveRange(_dbContext.DefenseQueue);
        _dbContext.ResearchQueue.RemoveRange(_dbContext.ResearchQueue);
        _dbContext.Buildings.RemoveRange(_dbContext.Buildings);
        _dbContext.Technologies.RemoveRange(_dbContext.Technologies);
        _dbContext.Ships.RemoveRange(_dbContext.Ships);
        _dbContext.Defenses.RemoveRange(_dbContext.Defenses);
        _dbContext.GameState.RemoveRange(_dbContext.GameState);
        _dbContext.PlayerPlanets.RemoveRange(_dbContext.PlayerPlanets);

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Game data reset");

        // Reinitialize
        await InitializeGameStateAsync();
    }

    public async Task DeleteDatabaseAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        _logger.LogInformation("Database deleted");
    }
}
