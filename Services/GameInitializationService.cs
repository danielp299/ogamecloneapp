using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

public class GameInitializationService
{
    private readonly GameDbContext _dbContext;
    private readonly ILogger<GameInitializationService> _logger;
    private readonly Random _random = new();
    
    // Servicios que inicializaremos
    private GalaxyService _galaxyService;
    private EnemyService _enemyService;
    private ResourceService _resourceService;
    private BuildingService _buildingService;
    private TechnologyService _technologyService;
    private FleetService _fleetService;
    private DefenseService _defenseService;

    public GameInitializationService(
        GameDbContext dbContext,
        ILogger<GameInitializationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task InitializeGameAsync(
        GalaxyService galaxyService,
        EnemyService enemyService,
        ResourceService resourceService,
        BuildingService buildingService,
        TechnologyService technologyService,
        FleetService fleetService,
        DefenseService defenseService,
        PlayerStateService playerStateService)
    {
        _galaxyService = galaxyService;
        _enemyService = enemyService;
        _resourceService = resourceService;
        _buildingService = buildingService;
        _technologyService = technologyService;
        _fleetService = fleetService;
        _defenseService = defenseService;

        // 1. Aplicar migraciones de BD
        await _dbContext.Database.MigrateAsync();
        _logger.LogInformation("Database migrated/verified");

        // 2. Verificar si existe una partida guardada válida
        var gameState = await _dbContext.GameState.FirstOrDefaultAsync();
        bool hasExistingGame = gameState != null && gameState.HomeGalaxy > 0;

        if (hasExistingGame)
        {
            _logger.LogInformation("Found existing game. Loading...");
            await LoadExistingGameAsync(gameState, playerStateService);
        }
        else
        {
            _logger.LogInformation("No existing game found. Creating new game...");
            await CreateNewGameAsync(playerStateService);
        }
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        await _dbContext.Database.MigrateAsync();
        _logger.LogInformation("Database migrated/verified");
    }

    public async Task ResetAndReinitializeGameAsync(
        PlayerStateService playerStateService)
    {
        _logger.LogInformation("Starting complete game reset...");
        
        // 1. Delete the entire database
        await _dbContext.Database.EnsureDeletedAsync();
        _logger.LogInformation("Database deleted successfully");
        
        // 2. Reset service states to initial values
        _galaxyService.ResetState();
        _enemyService.ResetState();
        _buildingService.ResetState();
        _technologyService.ResetState();
        _fleetService.ResetState();
        _defenseService.ResetState();
        _resourceService.ResetState();
        playerStateService.ResetState();
        _logger.LogInformation("All service states reset");
        
        // 3. Recreate database and initialize new game
        await _dbContext.Database.MigrateAsync();
        _logger.LogInformation("Database recreated");
        
        // 4. Create new game with fresh initialization
        await CreateNewGameAsync(playerStateService);
        
        _logger.LogInformation("Game reset and reinitialization completed successfully!");
    }

    private async Task LoadExistingGameAsync(GameState gameState, PlayerStateService playerStateService)
    {
        // Configurar GalaxyService con las coordenadas existentes
        _galaxyService.SetHomeCoordinates(
            gameState.HomeGalaxy,
            gameState.HomeSystem,
            gameState.HomePosition
        );

        var storedPlanets = await _dbContext.PlayerPlanets.ToListAsync();
        if (!storedPlanets.Any())
        {
            _dbContext.PlayerPlanets.Add(new PlayerPlanetEntity
            {
                Galaxy = gameState.HomeGalaxy,
                System = gameState.HomeSystem,
                Position = gameState.HomePosition,
                Name = "Homeworld",
                Image = "assets/planets/planet_home.jpg",
                IsHomeworld = true
            });
            await _dbContext.SaveChangesAsync();
            storedPlanets = await _dbContext.PlayerPlanets.ToListAsync();
        }

        foreach (var planet in storedPlanets.Where(p => !p.IsHomeworld))
        {
            _galaxyService.RegisterPlanet(new GalaxyPlanet
            {
                Galaxy = planet.Galaxy,
                System = planet.System,
                Position = planet.Position,
                Name = planet.Name,
                PlayerName = "Commander",
                Image = planet.Image,
                IsOccupied = true,
                IsMyPlanet = true,
                IsHomeworld = false
            });
        }

        _galaxyService.Initialize();
        _galaxyService.RefreshSystems();

        // Inicializar PlayerStateService (después de GalaxyService)
        playerStateService.Initialize();

        // Inicializar BuildingService (después de PlayerStateService)
        await _buildingService.InitializeAsync();

        // Inicializar EnemyService (cargará enemigos de BD)
        _enemyService.Initialize();

        _logger.LogInformation($"Game loaded successfully. Home: {gameState.HomeGalaxy}:{gameState.HomeSystem}:{gameState.HomePosition}");
    }

    private async Task CreateNewGameAsync(PlayerStateService playerStateService)
    {
        // 1. Generar coordenadas random para el planeta inicial
        int homeGalaxy = _random.Next(1, GalaxyService.MaxGalaxies + 1);
        int homeSystem = _random.Next(1, GalaxyService.MaxSystemsPerGalaxy + 1);
        int homePosition = _random.Next(1, 16); // 1-15

        _logger.LogInformation($"Generated home coordinates: {homeGalaxy}:{homeSystem}:{homePosition}");

        // 2. Crear GameState
        var gameState = new GameState
        {
            Id = 1,
            HomeGalaxy = homeGalaxy,
            HomeSystem = homeSystem,
            HomePosition = homePosition,
            Metal = 50000,
            Crystal = 50000,
            Deuterium = 50000,
            DarkMatter = 0,
            Energy = 0,
            DevModeEnabled = true,
            CreatedAt = DateTime.UtcNow,
            LastResourceUpdate = DateTime.UtcNow,
            LastSavedAt = DateTime.UtcNow
        };

        _dbContext.GameState.Add(gameState);

        // 3. Inicializar tecnologías globales
        await InitializeTechnologiesAsync();

        // 4. Guardar cambios iniciales
        await _dbContext.SaveChangesAsync();

        // 5. Configurar GalaxyService
        _galaxyService.SetHomeCoordinates(homeGalaxy, homeSystem, homePosition);
        _galaxyService.Initialize();

        // 6. Inicializar PlayerStateService (después de GalaxyService)
        playerStateService.Initialize();

        // 7. Inicializar estado del planeta del jugador (ANTES de BuildingService)
        await InitializePlayerPlanetAsync(homeGalaxy, homeSystem, homePosition);

        _dbContext.PlayerPlanets.Add(new PlayerPlanetEntity
        {
            Galaxy = homeGalaxy,
            System = homeSystem,
            Position = homePosition,
            Name = "Homeworld",
            Image = "assets/planets/planet_home.jpg",
            IsHomeworld = true
        });
        await _dbContext.SaveChangesAsync();

        // 8. Inicializar BuildingService (después de que el planeta existe)
        await _buildingService.InitializeAsync();

        // 9. Generar enemigos iniciales
        await _enemyService.GenerateInitialEnemiesAsync(
            homeGalaxy, homeSystem, homePosition
        );

        // 10. Recargar GalaxyService para que vea los enemigos
        _galaxyService.RefreshSystems();

        _logger.LogInformation("New game created successfully!");
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
            if (!await _dbContext.Technologies.AnyAsync(t => t.TechnologyType == type))
            {
                _dbContext.Technologies.Add(new TechnologyEntity
                {
                    TechnologyType = type,
                    Level = 0
                });
            }
        }

        _logger.LogInformation("Technologies initialized");
    }

    private async Task InitializePlayerPlanetAsync(int galaxy, int system, int position)
    {
        // 1. Crear PlanetState
        var planetState = new PlanetState
        {
            Galaxy = galaxy,
            System = system,
            Position = position,
            Metal = 500,
            Crystal = 500,
            Deuterium = 0
        };
        _dbContext.PlanetStates.Add(planetState);

        // 2. Inicializar edificios
        var buildingTypes = new[]
        {
            "Metal Mine", "Crystal Mine", "Deuterium Synthesizer", "Solar Plant",
            "Robotics Factory", "Fusion Reactor", "Alliance Depot", "Shipyard",
            "Metal Storage", "Crystal Storage", "Deuterium Tank", "Research Lab",
            "Terraformer", "Missile Silo", "Nanite Factory"
        };

        foreach (var type in buildingTypes)
        {
            _dbContext.Buildings.Add(new BuildingEntity
            {
                BuildingType = type,
                Level = 0,
                Galaxy = galaxy,
                System = system,
                Position = position
            });
        }

        // 3. Inicializar naves (vacías)
        var shipTypes = new[] { "SC", "LC", "LF", "HF", "CR", "BS", "CS", "REC", "ESP", "DST", "RIP" };
        foreach (var type in shipTypes)
        {
            _dbContext.Ships.Add(new ShipEntity
            {
                ShipType = type,
                Quantity = 0,
                Galaxy = galaxy,
                System = system,
                Position = position
            });
        }

        // 4. Inicializar defensas (vacías)
        var defenseTypes = new[] { "RL", "LL", "HL", "GC", "IC", "PT", "SSD", "LSD", "ABM", "IPM" };
        foreach (var type in defenseTypes)
        {
            _dbContext.Defenses.Add(new DefenseEntity
            {
                DefenseType = type,
                Quantity = 0,
                Galaxy = galaxy,
                System = system,
                Position = position
            });
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation($"Player planet initialized at {galaxy}:{system}:{position}");
    }
}
