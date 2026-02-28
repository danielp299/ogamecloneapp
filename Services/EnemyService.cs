using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

public class Enemy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }

    public Guid EmpireId { get; set; } = Guid.Empty;
    public bool IsHomeworld { get; set; } = false;
    
    // Resources
    public long Metal { get; set; } = 1000;
    public long Crystal { get; set; } = 500;
    public long Deuterium { get; set; } = 200;
    public long Energy { get; set; } = 0;
    
    // Production rates per second (calculated from buildings)
    public double MetalProductionRate { get; set; } = 0.5;
    public double CrystalProductionRate { get; set; } = 0.3;
    public double DeuteriumProductionRate { get; set; } = 0.1;
    
    // Buildings - Level 0-30
    public Dictionary<string, int> Buildings { get; set; } = new();
    
    // Technologies - Level 0-20
    public Dictionary<string, int> Technologies { get; set; } = new();
    
    // Defenses - Count
    public Dictionary<string, int> Defenses { get; set; } = new();
    
    // Ships - Count
    public Dictionary<string, int> Ships { get; set; } = new();
    
    // Last resource update time
    public DateTime LastResourceUpdate { get; set; } = DateTime.Now;
    
    // Last activity time (for determining actions)
    public DateTime LastActivity { get; set; } = DateTime.Now;
    
    // Alliance marker
    public bool IsBot { get; set; } = false;
    
    // Colony tracking
    public int ColonyCount { get; set; } = 0;

    // Strategic memory
    public HashSet<int> ExploredGalaxies { get; set; } = new();
    public List<string> KnownEnemyCoordinates { get; set; } = new();
    public Dictionary<string, long> SpiedEnemyPower { get; set; } = new();
    
    public string Coordinates => $"{Galaxy}:{System}:{Position}";
}

public class EnemyService
{
    private readonly GameDbContext _dbContext;
    private readonly GalaxyService _galaxyService;
    private readonly Random _random = new Random();
    private readonly object _lockObject = new object();
    
    // Enemy storage - key is coordinates "galaxy:system:position"
    private Dictionary<string, Enemy> _enemies = new();
    
    public List<Enemy> Enemies => _enemies.Values.ToList();
    
    public event Action? OnChange;
    public event Action<string, string, Dictionary<string, int>>? OnEnemyAttackLaunched;
    
    private const int MAX_ENEMIES = 100;
    private const double BUILDING_UPGRADE_CHANCE = 0.70; // 70%
    private const double RESEARCH_CHANCE = 0.80; // 80%
    private const double DEFENSE_BUILD_CHANCE = 0.60; // 60%
    private const double SHIP_BUILD_CHANCE = 0.50; // 50%
    private const double SPEND_RESOURCES_CHANCE = 0.40; // 40%
    private const double EXPLORE_DISCOVERY_CHANCE = 0.30;
    private const double ATTACK_POWER_ADVANTAGE = 1.20;
    private const int MAX_KNOWN_ENEMIES_MEMORY = 10;
    
    // Maximum actions per enemy per player action event
    private const int MAX_ACTIONS_PER_EVENT = 2; // Limit to 2 actions maximum per event
    
    // Available building types
    private static readonly string[] BuildingTypes = {
        "Metal Mine", "Crystal Mine", "Deuterium Synthesizer", "Solar Plant",
        "Robotics Factory", "Shipyard", "Research Lab", "Metal Storage",
        "Crystal Storage", "Deuterium Tank", "Fusion Reactor", "Nanite Factory"
    };
    
    // Available technology types
    private static readonly string[] TechnologyTypes = {
        "Espionage Technology", "Computer Technology", "Weapons Technology",
        "Shielding Technology", "Armour Technology", "Energy Technology",
        "Hyperspace Technology", "Combustion Drive", "Impulse Drive",
        "Hyperspace Drive", "Laser Technology", "Ion Technology", "Plasma Technology",
        "Intergalactic Research Network", "Astrophysics", "Graviton Technology"
    };
    
    // Available defense types
    private static readonly string[] DefenseTypes = {
        "Rocket Launcher", "Light Laser", "Heavy Laser", "Gauss Cannon",
        "Ion Cannon", "Plasma Turret", "Small Shield Dome", "Large Shield Dome",
        "Anti-Ballistic Missile"
    };
    
    // Available ship types
    private static readonly string[] ShipTypes = {
        "SC", "LC", "LF", "HF", "CR", "BS", "CS", "REC", "ESP", "DST", "RIP"
    };
    
    // ===== REQUIREMENTS DEFINITIONS =====
    // Building requirements - which buildings are needed to unlock others
    private static readonly Dictionary<string, Dictionary<string, int>> BuildingRequirements = new()
    {
        // All buildings available from start (no requirements) except:
        ["Fusion Reactor"] = new() { ["Deuterium Synthesizer"] = 5, ["Energy Technology"] = 3 },
        ["Nanite Factory"] = new() { ["Robotics Factory"] = 10, ["Computer Technology"] = 10 },
        // Shipyard requires Robotics Factory
        ["Shipyard"] = new() { ["Robotics Factory"] = 2 },
        // Research Lab requires no specific buildings
    };
    
    // Technology requirements - which buildings/techs needed to research
    private static readonly Dictionary<string, Dictionary<string, int>> TechnologyRequirements = new()
    {
        ["Espionage Technology"] = new() { ["Research Lab"] = 3 },
        ["Computer Technology"] = new() { ["Research Lab"] = 1 },
        ["Weapons Technology"] = new() { ["Research Lab"] = 4 },
        ["Shielding Technology"] = new() { ["Research Lab"] = 6, ["Energy Technology"] = 3 },
        ["Armour Technology"] = new() { ["Research Lab"] = 2 },
        ["Energy Technology"] = new() { ["Research Lab"] = 1 },
        ["Hyperspace Technology"] = new() { ["Research Lab"] = 7, ["Energy Technology"] = 5, ["Shielding Technology"] = 5 },
        ["Combustion Drive"] = new() { ["Research Lab"] = 1, ["Energy Technology"] = 1 },
        ["Impulse Drive"] = new() { ["Research Lab"] = 2, ["Energy Technology"] = 1 },
        ["Hyperspace Drive"] = new() { ["Research Lab"] = 7, ["Hyperspace Technology"] = 3 },
        ["Laser Technology"] = new() { ["Research Lab"] = 1, ["Energy Technology"] = 2 },
        ["Ion Technology"] = new() { ["Research Lab"] = 4, ["Laser Technology"] = 5, ["Energy Technology"] = 4 },
        ["Plasma Technology"] = new() { ["Research Lab"] = 4, ["Energy Technology"] = 8, ["Laser Technology"] = 10, ["Ion Technology"] = 5 },
        ["Intergalactic Research Network"] = new() { ["Research Lab"] = 10, ["Computer Technology"] = 8, ["Hyperspace Technology"] = 8 },
        ["Astrophysics"] = new() { ["Research Lab"] = 3, ["Espionage Technology"] = 4, ["Impulse Drive"] = 3 },
        ["Graviton Technology"] = new() { ["Research Lab"] = 12 }
    };
    
    // Ship requirements - which buildings and techs needed to build ships
    private static readonly Dictionary<string, Dictionary<string, int>> ShipRequirements = new()
    {
        ["SC"] = new() { ["Shipyard"] = 2, ["Combustion Drive"] = 2 },
        ["LC"] = new() { ["Shipyard"] = 4, ["Combustion Drive"] = 6 },
        ["LF"] = new() { ["Shipyard"] = 1, ["Combustion Drive"] = 1 },
        ["HF"] = new() { ["Shipyard"] = 3, ["Impulse Drive"] = 2 },
        ["CR"] = new() { ["Shipyard"] = 5, ["Impulse Drive"] = 4, ["Ion Technology"] = 2 },
        ["BS"] = new() { ["Shipyard"] = 7, ["Hyperspace Drive"] = 4 },
        ["CS"] = new() { ["Shipyard"] = 4, ["Impulse Drive"] = 3 },
        ["REC"] = new() { ["Shipyard"] = 4, ["Combustion Drive"] = 6, ["Shielding Technology"] = 2 },
        ["ESP"] = new() { ["Shipyard"] = 3, ["Espionage Technology"] = 2 },
        ["DST"] = new() { ["Shipyard"] = 9, ["Hyperspace Drive"] = 3, ["Hyperspace Technology"] = 5 },
        ["RIP"] = new() { ["Shipyard"] = 12, ["Hyperspace Drive"] = 6, ["Hyperspace Technology"] = 5, ["Graviton Technology"] = 1 }
    };
    
    // Defense requirements - which buildings and techs needed to build defenses
    private static readonly Dictionary<string, Dictionary<string, int>> DefenseRequirements = new()
    {
        ["Rocket Launcher"] = new() { ["Shipyard"] = 1 },
        ["Light Laser"] = new() { ["Shipyard"] = 2, ["Laser Technology"] = 3 },
        ["Heavy Laser"] = new() { ["Shipyard"] = 4, ["Laser Technology"] = 6 },
        ["Gauss Cannon"] = new() { ["Shipyard"] = 6, ["Energy Technology"] = 6, ["Weapons Technology"] = 3 },
        ["Ion Cannon"] = new() { ["Shipyard"] = 4, ["Ion Technology"] = 4 },
        ["Plasma Turret"] = new() { ["Shipyard"] = 8, ["Plasma Technology"] = 7 },
        ["Small Shield Dome"] = new() { ["Shipyard"] = 1, ["Shielding Technology"] = 2 },
        ["Large Shield Dome"] = new() { ["Shipyard"] = 6, ["Shielding Technology"] = 6 },
        ["Anti-Ballistic Missile"] = new() { ["Shipyard"] = 1, ["Missile Silo"] = 2 }
    };
    
    private bool _isInitialized = false;
    private bool _enemyMemoryColumnsReady = false;

    public EnemyService(GameDbContext dbContext, GalaxyService galaxyService)
    {
        _dbContext = dbContext;
        _galaxyService = galaxyService;
        // NOTA: La inicialización es lazy via Initialize() o GenerateInitialEnemiesAsync()
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        await LoadEnemiesAsync();
        _isInitialized = true;
    }

    public void ResetState()
    {
        lock (_lockObject)
        {
            _enemies.Clear();
            _isInitialized = false;
        }
    }
    
    private async Task LoadEnemiesAsync()
    {
        try
        {
            await EnsureEnemyMemoryColumnsAsync();
            var dbEnemies = await _dbContext.Enemies.ToListAsync();
            
            if (dbEnemies.Any())
            {
                // Load existing enemies
                foreach (var dbEnemy in dbEnemies)
                {
                    var enemy = new Enemy
                    {
                        Id = dbEnemy.Id,
                        Name = dbEnemy.Name,
                        Galaxy = dbEnemy.Galaxy,
                        System = dbEnemy.System,
                        Position = dbEnemy.Position,
                        EmpireId = dbEnemy.EmpireId == Guid.Empty ? dbEnemy.Id : dbEnemy.EmpireId,
                        IsHomeworld = dbEnemy.IsHomeworld,
                        Metal = dbEnemy.Metal,
                        Crystal = dbEnemy.Crystal,
                        Deuterium = dbEnemy.Deuterium,
                        Energy = dbEnemy.Energy,
                        LastResourceUpdate = dbEnemy.LastResourceUpdate,
                        LastActivity = dbEnemy.LastActivity,
                        IsBot = dbEnemy.IsBot,
                        ColonyCount = dbEnemy.ColonyCount
                    };

                    if (enemy.EmpireId == enemy.Id)
                    {
                        enemy.IsHomeworld = true;
                    }
                    
                    // Deserialize buildings
                    if (!string.IsNullOrEmpty(dbEnemy.BuildingsJson))
                    {
                        enemy.Buildings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(dbEnemy.BuildingsJson) ?? new();
                    }
                    
                    // Deserialize technologies
                    if (!string.IsNullOrEmpty(dbEnemy.TechnologiesJson))
                    {
                        enemy.Technologies = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(dbEnemy.TechnologiesJson) ?? new();
                    }
                    
                    // Deserialize defenses
                    if (!string.IsNullOrEmpty(dbEnemy.DefensesJson))
                    {
                        enemy.Defenses = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(dbEnemy.DefensesJson) ?? new();
                    }
                    
                    // Deserialize ships
                    if (!string.IsNullOrEmpty(dbEnemy.ShipsJson))
                    {
                        enemy.Ships = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(dbEnemy.ShipsJson) ?? new();
                    }

                    if (!string.IsNullOrEmpty(dbEnemy.ExploredGalaxiesJson))
                    {
                        enemy.ExploredGalaxies = System.Text.Json.JsonSerializer.Deserialize<HashSet<int>>(dbEnemy.ExploredGalaxiesJson) ?? new();
                    }

                    if (!string.IsNullOrEmpty(dbEnemy.KnownEnemyCoordinatesJson))
                    {
                        enemy.KnownEnemyCoordinates = System.Text.Json.JsonSerializer.Deserialize<List<string>>(dbEnemy.KnownEnemyCoordinatesJson) ?? new();
                    }

                    if (!string.IsNullOrEmpty(dbEnemy.SpiedEnemyPowerJson))
                    {
                        enemy.SpiedEnemyPower = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, long>>(dbEnemy.SpiedEnemyPowerJson) ?? new();
                    }
                    
                    // Update production rates
                    UpdateEnemyProductionRates(enemy);
                    
                    var key = $"{enemy.Galaxy}:{enemy.System}:{enemy.Position}";
                    _enemies[key] = enemy;
                }

                Console.WriteLine($"Loaded {dbEnemies.Count} enemies from database");
            }

            foreach (var group in _enemies.Values.GroupBy(e => e.EmpireId))
            {
                int colonyCount = group.Count(e => !e.IsHomeworld);
                foreach (var enemy in group)
                {
                    enemy.ColonyCount = colonyCount;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading enemies: {ex.Message}");
            // Don't generate enemies here - let GameInitializationService handle it
        }
    }

    public async Task GenerateInitialEnemiesAsync(int playerGalaxy, int playerSystem, int playerPosition)
    {
        if (_isInitialized) return;

        int enemiesCreated = 0;
        
        lock (_lockObject)
        {
            _enemies.Clear();
            
            int attempts = 0;
            int maxAttempts = MAX_ENEMIES * 10; // Prevent infinite loop
            
            while (enemiesCreated < MAX_ENEMIES && attempts < maxAttempts)
            {
                attempts++;
                
                // Random position within universe limits
                int galaxy = _random.Next(1, GalaxyService.MaxGalaxies + 1);
                int system = _random.Next(1, GalaxyService.MaxSystemsPerGalaxy + 1);
                int position = _random.Next(1, 16); // 1-15
                
                var key = $"{galaxy}:{system}:{position}";
                
                // Skip if this position is already occupied by an enemy
                if (_enemies.ContainsKey(key))
                    continue;
                
                // Skip if this is the player's home planet
                if (galaxy == playerGalaxy && system == playerSystem && position == playerPosition)
                    continue;
                
                // Create enemy
                var enemy = new Enemy
                {
                    Name = $"Enemy_{_random.Next(1000, 9999)}",
                    Galaxy = galaxy,
                    System = system,
                    Position = position,
                    IsBot = true
                };

                enemy.EmpireId = enemy.Id;
                enemy.IsHomeworld = true;
                
                // Initialize buildings at level 0-1 (same as starting player level)
                foreach (var buildingType in BuildingTypes)
                {
                    enemy.Buildings[buildingType] = _random.Next(0, 2); // 0 or 1
                }
                
                // Ensure basic buildings exist
                if (enemy.Buildings["Metal Mine"] == 0) enemy.Buildings["Metal Mine"] = 1;
                if (enemy.Buildings["Solar Plant"] == 0) enemy.Buildings["Solar Plant"] = 1;
                
                // Initialize technologies at level 0
                foreach (var techType in TechnologyTypes)
                {
                    enemy.Technologies[techType] = 0;
                }
                
                // Give some initial defenses
                enemy.Defenses["Rocket Launcher"] = _random.Next(0, 5);
                
                // Give some initial ships
                enemy.Ships["SC"] = _random.Next(0, 3);
                enemy.Ships["LF"] = _random.Next(0, 3);
                
                // Update production rates
                UpdateEnemyProductionRates(enemy);
                
                _enemies[key] = enemy;
                enemiesCreated++;
            }
        }
        
        // Save to database
        await SaveEnemiesAsync();
        
        _isInitialized = true;
        NotifyStateChanged();
        Console.WriteLine($"Generated {enemiesCreated} initial enemies");
    }
    
    private void UpdateEnemyProductionRates(Enemy enemy)
    {
        double speedMultiplier = 1000.0;
        
        // Calculate production based on building levels
        int metalMineLevel = enemy.Buildings.GetValueOrDefault("Metal Mine", 0);
        int crystalMineLevel = enemy.Buildings.GetValueOrDefault("Crystal Mine", 0);
        int deuteriumSynthLevel = enemy.Buildings.GetValueOrDefault("Deuterium Synthesizer", 0);
        int solarPlantLevel = enemy.Buildings.GetValueOrDefault("Solar Plant", 0);
        
        // Energy calculation
        long energyProduction = 0;
        if (solarPlantLevel > 0)
        {
            energyProduction = (long)(20 * solarPlantLevel * Math.Pow(1.1, solarPlantLevel));
        }
        
        long energyConsumption = 0;
        if (metalMineLevel > 0)
            energyConsumption += (long)(10 * metalMineLevel * Math.Pow(1.1, metalMineLevel));
        if (crystalMineLevel > 0)
            energyConsumption += (long)(10 * crystalMineLevel * Math.Pow(1.1, crystalMineLevel));
        if (deuteriumSynthLevel > 0)
            energyConsumption += (long)(20 * deuteriumSynthLevel * Math.Pow(1.1, deuteriumSynthLevel));
        
        enemy.Energy = energyProduction - energyConsumption;
        
        // Production factor based on energy
        double productionFactor = 1.0;
        if (enemy.Energy < 0 && energyConsumption > 0)
        {
            productionFactor = (double)energyProduction / energyConsumption;
        }
        
        // Calculate rates per second
        if (metalMineLevel > 0)
        {
            double rawMetal = 30 * metalMineLevel * Math.Pow(1.1, metalMineLevel);
            enemy.MetalProductionRate = (rawMetal * productionFactor * speedMultiplier) / 3600.0;
        }
        else
        {
            enemy.MetalProductionRate = (30.0 * speedMultiplier) / 3600.0; // Base production
        }
        
        if (crystalMineLevel > 0)
        {
            double rawCrystal = 20 * crystalMineLevel * Math.Pow(1.1, crystalMineLevel);
            enemy.CrystalProductionRate = (rawCrystal * productionFactor * speedMultiplier) / 3600.0;
        }
        else
        {
            enemy.CrystalProductionRate = (15.0 * speedMultiplier) / 3600.0; // Base production
        }
        
        if (deuteriumSynthLevel > 0)
        {
            double rawDeuterium = 10 * deuteriumSynthLevel * Math.Pow(1.1, deuteriumSynthLevel);
            enemy.DeuteriumProductionRate = (rawDeuterium * productionFactor * speedMultiplier) / 3600.0;
        }
        else
        {
            enemy.DeuteriumProductionRate = 0;
        }
    }
    
    // Update resources for a single enemy based on time elapsed since last update
    private void UpdateEnemyResources(Enemy enemy)
    {
        var now = DateTime.Now;
        var timeSinceLastUpdate = now - enemy.LastResourceUpdate;
        double seconds = timeSinceLastUpdate.TotalSeconds;
        
        if (seconds > 0)
        {
            // Update resources based on production rates
            enemy.Metal += (long)(enemy.MetalProductionRate * seconds);
            enemy.Crystal += (long)(enemy.CrystalProductionRate * seconds);
            enemy.Deuterium += (long)(enemy.DeuteriumProductionRate * seconds);
            
            // Cap resources at storage limits
            long metalStorage = GetStorageCapacity(enemy, "Metal Storage");
            long crystalStorage = GetStorageCapacity(enemy, "Crystal Storage");
            long deuteriumStorage = GetStorageCapacity(enemy, "Deuterium Tank");
            
            if (enemy.Metal > metalStorage) enemy.Metal = metalStorage;
            if (enemy.Crystal > crystalStorage) enemy.Crystal = crystalStorage;
            if (enemy.Deuterium > deuteriumStorage) enemy.Deuterium = deuteriumStorage;
            
            enemy.LastResourceUpdate = now;
        }
    }
    
    private long GetStorageCapacity(Enemy enemy, string storageType)
    {
        int level = enemy.Buildings.GetValueOrDefault(storageType, 0);
        if (level == 0) return 10000; // Base storage without storage building
        
        // OGame storage formula
        return (long)(10000 * Math.Pow(1.6, level));
    }

    private List<Enemy> GetEmpirePlanets(Enemy enemy)
    {
        return _enemies.Values.Where(e => e.EmpireId == enemy.EmpireId).ToList();
    }

    private void UpdateEmpireColonyCount(Guid empireId)
    {
        var empirePlanets = _enemies.Values.Where(e => e.EmpireId == empireId).ToList();
        int colonyCount = empirePlanets.Count(e => !e.IsHomeworld);
        foreach (var planet in empirePlanets)
        {
            planet.ColonyCount = colonyCount;
        }
    }

    private void ExecuteAttackReaction(Enemy enemy)
    {
        UpdateEnemyResources(enemy);

        var availableDefenses = GetAvailableDefenses(enemy);
        var availableShips = GetAvailableShips(enemy);
        var availableBuildings = GetAvailableBuildings(enemy);
        var availableTechs = GetAvailableTechnologies(enemy);

        int actionsToPerform = _random.Next(1, Math.Min(3, MAX_ACTIONS_PER_EVENT + 1));
        int actionsPerformed = 0;

        var possibleActions = new List<string>();
        if (availableDefenses.Any()) possibleActions.Add("defense");
        if (availableShips.Any()) possibleActions.Add("ship");
        if (availableBuildings.Any()) possibleActions.Add("building");
        if (availableTechs.Any()) possibleActions.Add("research");
        possibleActions.Add("explore");
        if (enemy.KnownEnemyCoordinates.Any()) possibleActions.Add("spy");
        if (enemy.SpiedEnemyPower.Any()) possibleActions.Add("attack");

        while (actionsPerformed < actionsToPerform && possibleActions.Any())
        {
            string actionType = possibleActions[_random.Next(possibleActions.Count)];
            bool actionSuccess = false;

            switch (actionType)
            {
                case "defense":
                    string defenseType = availableDefenses[_random.Next(availableDefenses.Count)];
                    actionSuccess = TryBuildDefense(enemy, defenseType);
                    break;

                case "ship":
                    string shipType = availableShips[_random.Next(availableShips.Count)];
                    actionSuccess = TryBuildShip(enemy, shipType);
                    break;

                case "building":
                    string building = availableBuildings[_random.Next(availableBuildings.Count)];
                    actionSuccess = TryUpgradeBuilding(enemy, building);
                    if (actionSuccess) UpdateEnemyProductionRates(enemy);
                    break;

                case "research":
                    string tech = availableTechs[_random.Next(availableTechs.Count)];
                    actionSuccess = TryResearchTechnology(enemy, tech);
                    break;
                case "explore":
                    actionSuccess = TryExploreGalaxy(enemy);
                    break;
                case "spy":
                    actionSuccess = TrySpyKnownEnemy(enemy);
                    break;
                case "attack":
                    actionSuccess = TryLaunchAttack(enemy);
                    break;
            }

            if (actionSuccess)
            {
                actionsPerformed++;
            }
            else
            {
                possibleActions.Remove(actionType);
            }
        }

        enemy.LastActivity = DateTime.Now;
    }

    private void ExecuteEmpireAction(Enemy enemy, string? preferredBuilding, string? preferredTech, string? preferredShip, string? preferredDefense)
    {
        var availableBuildings = GetAvailableBuildings(enemy);
        var availableTechs = GetAvailableTechnologies(enemy);
        var availableDefenses = GetAvailableDefenses(enemy);
        var availableShips = GetAvailableShips(enemy);

        int actionsToPerform = _random.Next(1, Math.Min(3, MAX_ACTIONS_PER_EVENT + 1));
        int actionsPerformed = 0;

        var possibleActions = new List<string>();
        if (availableBuildings.Any()) possibleActions.Add("building");
        if (availableTechs.Any()) possibleActions.Add("research");
        if (availableDefenses.Any()) possibleActions.Add("defense");
        if (availableShips.Any()) possibleActions.Add("ship");
        possibleActions.Add("explore");
        if (enemy.KnownEnemyCoordinates.Any()) possibleActions.Add("spy");
        if (enemy.SpiedEnemyPower.Any()) possibleActions.Add("attack");

        while (actionsPerformed < actionsToPerform && possibleActions.Any())
        {
            string actionType = possibleActions[_random.Next(possibleActions.Count)];
            bool actionSuccess = false;

            switch (actionType)
            {
                case "building":
                    string buildingToUpgrade;
                    bool sameBuildingAvailable = !string.IsNullOrEmpty(preferredBuilding) &&
                                                 availableBuildings.Contains(preferredBuilding);

                    if (_random.NextDouble() < 0.5 && sameBuildingAvailable)
                    {
                        buildingToUpgrade = preferredBuilding!;
                    }
                    else
                    {
                        buildingToUpgrade = availableBuildings[_random.Next(availableBuildings.Count)];
                    }

                    actionSuccess = TryUpgradeBuilding(enemy, buildingToUpgrade);
                    if (actionSuccess)
                    {
                        UpdateEnemyProductionRates(enemy);
                    }
                    break;

                case "research":
                    string techToResearch;
                    bool sameTechAvailable = !string.IsNullOrEmpty(preferredTech) &&
                                             availableTechs.Contains(preferredTech);

                    if (_random.NextDouble() < 0.5 && sameTechAvailable)
                    {
                        techToResearch = preferredTech!;
                    }
                    else
                    {
                        techToResearch = availableTechs[_random.Next(availableTechs.Count)];
                    }

                    actionSuccess = TryResearchTechnology(enemy, techToResearch);
                    break;

                case "defense":
                    string defenseToBuild;
                    bool sameDefenseAvailable = !string.IsNullOrEmpty(preferredDefense) &&
                                                availableDefenses.Contains(preferredDefense);

                    if (_random.NextDouble() < 0.5 && sameDefenseAvailable)
                    {
                        defenseToBuild = preferredDefense!;
                    }
                    else
                    {
                        defenseToBuild = availableDefenses[_random.Next(availableDefenses.Count)];
                    }

                    actionSuccess = TryBuildDefense(enemy, defenseToBuild);
                    break;

                case "ship":
                    string shipToBuild;
                    bool sameShipAvailable = !string.IsNullOrEmpty(preferredShip) &&
                                             availableShips.Contains(preferredShip);

                    if (_random.NextDouble() < 0.5 && sameShipAvailable)
                    {
                        shipToBuild = preferredShip!;
                    }
                    else
                    {
                        shipToBuild = availableShips[_random.Next(availableShips.Count)];
                    }

                    actionSuccess = TryBuildShip(enemy, shipToBuild);
                    break;
                case "explore":
                    actionSuccess = TryExploreGalaxy(enemy);
                    break;
                case "spy":
                    actionSuccess = TrySpyKnownEnemy(enemy);
                    break;
                case "attack":
                    actionSuccess = TryLaunchAttack(enemy);
                    break;
            }

            if (actionSuccess)
            {
                actionsPerformed++;
            }
            else
            {
                possibleActions.Remove(actionType);
            }
        }

        enemy.LastActivity = DateTime.Now;
    }
    
    // ===== REQUIREMENTS CHECKING METHODS =====
    
    // Check if enemy meets requirements for a building
    private bool CanEnemyUpgradeBuilding(Enemy enemy, string buildingName)
    {
        // Check if building has specific requirements
        if (BuildingRequirements.ContainsKey(buildingName))
        {
            var requirements = BuildingRequirements[buildingName];
            foreach (var req in requirements)
            {
                string reqName = req.Key;
                int reqLevel = req.Value;
                
                // Check if requirement is a building
                if (enemy.Buildings.ContainsKey(reqName))
                {
                    if (enemy.Buildings[reqName] < reqLevel)
                        return false;
                }
                // Check if requirement is a technology
                else if (enemy.Technologies.ContainsKey(reqName))
                {
                    if (enemy.Technologies[reqName] < reqLevel)
                        return false;
                }
                else
                {
                    // Requirement doesn't exist for enemy
                    return false;
                }
            }
        }
        
        // Most basic buildings don't have requirements
        return true;
    }
    
    // Check if enemy meets requirements for a technology
    private bool CanEnemyResearchTechnology(Enemy enemy, string techName)
    {
        // Must have Research Lab at least level 1 for any technology
        if (!enemy.Buildings.ContainsKey("Research Lab") || enemy.Buildings["Research Lab"] < 1)
            return false;
        
        // Check specific requirements
        if (TechnologyRequirements.ContainsKey(techName))
        {
            var requirements = TechnologyRequirements[techName];
            foreach (var req in requirements)
            {
                string reqName = req.Key;
                int reqLevel = req.Value;
                
                // Check buildings
                if (enemy.Buildings.ContainsKey(reqName))
                {
                    if (enemy.Buildings[reqName] < reqLevel)
                        return false;
                }
                // Check technologies
                else if (enemy.Technologies.ContainsKey(reqName))
                {
                    if (enemy.Technologies[reqName] < reqLevel)
                        return false;
                }
                else
                {
                    // Requirement doesn't exist
                    return false;
                }
            }
        }
        
        return true;
    }
    
    // Check if enemy meets requirements for a ship
    private bool CanEnemyBuildShip(Enemy enemy, string shipType)
    {
        // Must have Shipyard at least level 1 for any ship
        if (!enemy.Buildings.ContainsKey("Shipyard") || enemy.Buildings["Shipyard"] < 1)
            return false;
        
        // Check specific requirements
        if (ShipRequirements.ContainsKey(shipType))
        {
            var requirements = ShipRequirements[shipType];
            foreach (var req in requirements)
            {
                string reqName = req.Key;
                int reqLevel = req.Value;
                
                // Check buildings
                if (enemy.Buildings.ContainsKey(reqName))
                {
                    if (enemy.Buildings[reqName] < reqLevel)
                        return false;
                }
                // Check technologies
                else if (enemy.Technologies.ContainsKey(reqName))
                {
                    if (enemy.Technologies[reqName] < reqLevel)
                        return false;
                }
                else
                {
                    // Requirement doesn't exist
                    return false;
                }
            }
        }
        
        return true;
    }
    
    // Check if enemy meets requirements for a defense
    private bool CanEnemyBuildDefense(Enemy enemy, string defenseType)
    {
        // Must have Shipyard at least level 1 for any defense
        if (!enemy.Buildings.ContainsKey("Shipyard") || enemy.Buildings["Shipyard"] < 1)
            return false;
        
        // Check specific requirements
        if (DefenseRequirements.ContainsKey(defenseType))
        {
            var requirements = DefenseRequirements[defenseType];
            foreach (var req in requirements)
            {
                string reqName = req.Key;
                int reqLevel = req.Value;
                
                // Check buildings
                if (enemy.Buildings.ContainsKey(reqName))
                {
                    if (enemy.Buildings[reqName] < reqLevel)
                        return false;
                }
                // Check technologies
                else if (enemy.Technologies.ContainsKey(reqName))
                {
                    if (enemy.Technologies[reqName] < reqLevel)
                        return false;
                }
                else
                {
                    // Requirement doesn't exist
                    return false;
                }
            }
        }
        
        return true;
    }
    
    // Get whitelist of available buildings for an enemy
    private List<string> GetAvailableBuildings(Enemy enemy)
    {
        var available = new List<string>();
        
        foreach (var building in BuildingTypes)
        {
            if (CanEnemyUpgradeBuilding(enemy, building))
            {
                available.Add(building);
            }
        }
        
        return available;
    }
    
    // Get whitelist of available technologies for an enemy
    private List<string> GetAvailableTechnologies(Enemy enemy)
    {
        var available = new List<string>();
        
        foreach (var tech in TechnologyTypes)
        {
            if (CanEnemyResearchTechnology(enemy, tech))
            {
                available.Add(tech);
            }
        }
        
        return available;
    }
    
    // Get whitelist of available ships for an enemy
    private List<string> GetAvailableShips(Enemy enemy)
    {
        var available = new List<string>();
        
        foreach (var ship in ShipTypes)
        {
            if (CanEnemyBuildShip(enemy, ship))
            {
                available.Add(ship);
            }
        }
        
        return available;
    }
    
    // Get whitelist of available defenses for an enemy
    private List<string> GetAvailableDefenses(Enemy enemy)
    {
        var available = new List<string>();
        
        foreach (var defense in DefenseTypes)
        {
            if (CanEnemyBuildDefense(enemy, defense))
            {
                available.Add(defense);
            }
        }
        
        return available;
    }
    
    private async Task SaveEnemiesAsync()
    {
        try
        {
            await EnsureEnemyMemoryColumnsAsync();
            var existingEnemies = await _dbContext.Enemies.ToListAsync();
            var existingIds = existingEnemies.Select(e => e.Id).ToHashSet();
            
            foreach (var enemy in _enemies.Values)
            {
                var dbEnemy = existingEnemies.FirstOrDefault(e => e.Id == enemy.Id);
                
                if (dbEnemy == null)
                {
                    // Create new
                    dbEnemy = new EnemyEntity
                    {
                        Id = enemy.Id,
                        Name = enemy.Name,
                        Galaxy = enemy.Galaxy,
                        System = enemy.System,
                        Position = enemy.Position,
                        EmpireId = enemy.EmpireId,
                        IsHomeworld = enemy.IsHomeworld,
                        Metal = enemy.Metal,
                        Crystal = enemy.Crystal,
                        Deuterium = enemy.Deuterium,
                        Energy = enemy.Energy,
                        LastResourceUpdate = enemy.LastResourceUpdate,
                        LastActivity = enemy.LastActivity,
                        IsBot = enemy.IsBot,
                        ColonyCount = enemy.ColonyCount
                    };
                    _dbContext.Enemies.Add(dbEnemy);
                }
                else
                {
                    // Update existing
                    dbEnemy.Name = enemy.Name;
                    dbEnemy.Metal = enemy.Metal;
                    dbEnemy.Crystal = enemy.Crystal;
                    dbEnemy.Deuterium = enemy.Deuterium;
                    dbEnemy.Energy = enemy.Energy;
                    dbEnemy.LastResourceUpdate = enemy.LastResourceUpdate;
                    dbEnemy.LastActivity = enemy.LastActivity;
                    dbEnemy.ColonyCount = enemy.ColonyCount;
                    dbEnemy.EmpireId = enemy.EmpireId;
                    dbEnemy.IsHomeworld = enemy.IsHomeworld;
                }
                
                // Serialize dictionaries
                dbEnemy.BuildingsJson = System.Text.Json.JsonSerializer.Serialize(enemy.Buildings);
                dbEnemy.TechnologiesJson = System.Text.Json.JsonSerializer.Serialize(enemy.Technologies);
                dbEnemy.DefensesJson = System.Text.Json.JsonSerializer.Serialize(enemy.Defenses);
                dbEnemy.ShipsJson = System.Text.Json.JsonSerializer.Serialize(enemy.Ships);
                dbEnemy.ExploredGalaxiesJson = System.Text.Json.JsonSerializer.Serialize(enemy.ExploredGalaxies);
                dbEnemy.KnownEnemyCoordinatesJson = System.Text.Json.JsonSerializer.Serialize(enemy.KnownEnemyCoordinates);
                dbEnemy.SpiedEnemyPowerJson = System.Text.Json.JsonSerializer.Serialize(enemy.SpiedEnemyPower);
            }
            
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving enemies: {ex.Message}");
        }
    }

    private async Task EnsureEnemyMemoryColumnsAsync()
    {
        if (_enemyMemoryColumnsReady) return;
        if (!_dbContext.Database.IsSqlite()) return;

        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info('Enemies')";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingColumns.Add(reader.GetString(1));
            }
        }

        if (!existingColumns.Contains("ExploredGalaxiesJson"))
        {
            await _dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Enemies ADD COLUMN ExploredGalaxiesJson TEXT NOT NULL DEFAULT '[]'");
        }

        if (!existingColumns.Contains("KnownEnemyCoordinatesJson"))
        {
            await _dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Enemies ADD COLUMN KnownEnemyCoordinatesJson TEXT NOT NULL DEFAULT '[]'");
        }

        if (!existingColumns.Contains("SpiedEnemyPowerJson"))
        {
            await _dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Enemies ADD COLUMN SpiedEnemyPowerJson TEXT NOT NULL DEFAULT '{}'");
        }

        _enemyMemoryColumnsReady = true;
    }
    
    // Triggered when player upgrades a building - 70% of enemies upgrade buildings
    public async Task OnPlayerBuildingUpgraded(string buildingName)
    {
        List<IGrouping<Guid, Enemy>> enemyEmpires;
        lock (_lockObject)
        {
            enemyEmpires = _enemies.Values.GroupBy(e => e.EmpireId).ToList();
        }

        foreach (var empire in enemyEmpires)
        {
            bool empireActs = _random.NextDouble() < BUILDING_UPGRADE_CHANCE;
            foreach (var enemy in empire)
            {
                UpdateEnemyResources(enemy);
                if (empireActs)
                {
                    ExecuteEmpireAction(enemy, buildingName, null, null, null);
                }
            }

            if (_random.NextDouble() < 0.05)
            {
                var homeworld = empire.FirstOrDefault(e => e.IsHomeworld) ?? empire.First();
                await TryEnemyColonize(homeworld);
            }
        }
        
        await SaveEnemiesAsync();
        NotifyStateChanged();
    }

    private async Task TryEnemyColonize(Enemy parentEnemy)
    {
        // Check if enemy has Astrophysics technology
        if (!parentEnemy.Technologies.ContainsKey("Astrophysics") || parentEnemy.Technologies["Astrophysics"] < 1)
        {
            Console.WriteLine($"Enemy {parentEnemy.Name} cannot colonize - lacks Astrophysics technology");
            return;
        }
        
        // Check if enemy has at least 1 Colony Ship
        if (!parentEnemy.Ships.ContainsKey("CS") || parentEnemy.Ships["CS"] < 1)
        {
            Console.WriteLine($"Enemy {parentEnemy.Name} cannot colonize - no Colony Ships available");
            return;
        }
        
        // Check planet limit based on Astrophysics level
        int astroLevel = parentEnemy.Technologies["Astrophysics"];
        int maxColonies = (int)Math.Ceiling(astroLevel / 2.0);

        int currentColonies = GetEmpirePlanets(parentEnemy).Count(e => !e.IsHomeworld);
        if (currentColonies >= maxColonies)
        {
            Console.WriteLine($"Enemy {parentEnemy.Name} cannot colonize - limit reached ({currentColonies}/{maxColonies} colonies, Astrophysics {astroLevel})");
            return;
        }
        
        // Find an empty spot
        int galaxy = parentEnemy.Galaxy;
        int system = parentEnemy.System;
        
        // Try positions around the parent
        for (int p = 1; p <= 15; p++)
        {
            var key = $"{galaxy}:{system}:{p}";
            if (!_enemies.ContainsKey(key))
            {
                var planet = _galaxyService.GetPlanet(galaxy, system, p);
                if (planet != null && !planet.IsOccupied)
                {
                    // Consume Colony Ship
                    parentEnemy.Ships["CS"]--;
                    if (parentEnemy.Ships["CS"] <= 0)
                    {
                        parentEnemy.Ships.Remove("CS");
                    }
                    
                    // Colonize!
                    var newEnemy = new Enemy
                    {
                        Name = $"{parentEnemy.Name}_Colony",
                        Galaxy = galaxy,
                        System = system,
                        Position = p,
                        IsBot = true,
                        EmpireId = parentEnemy.EmpireId,
                        IsHomeworld = false
                    };
                    
                    // Basic buildings
                    newEnemy.Buildings["Metal Mine"] = 1;
                    newEnemy.Buildings["Solar Plant"] = 1;
                    UpdateEnemyProductionRates(newEnemy);
                    
                    _enemies[key] = newEnemy;
                    
                    // Update GalaxyService
                    planet.IsOccupied = true;
                    planet.IsMyPlanet = false;
                    planet.Name = newEnemy.Name;
                    planet.PlayerName = newEnemy.Name;
                    planet.Alliance = "[BOTS]";
                    
                    UpdateEmpireColonyCount(parentEnemy.EmpireId);
                    
                    Console.WriteLine($"Enemy {parentEnemy.Name} colonized {key} (Colony {currentColonies + 1}/{maxColonies}, Astrophysics {astroLevel})");
                    break;
                }
            }
        }
    }
    public async Task OnPlayerTechnologyResearched(string techName)
    {
        List<IGrouping<Guid, Enemy>> enemyEmpires;
        lock (_lockObject)
        {
            enemyEmpires = _enemies.Values.GroupBy(e => e.EmpireId).ToList();
        }

        foreach (var empire in enemyEmpires)
        {
            bool empireActs = _random.NextDouble() < RESEARCH_CHANCE;
            foreach (var enemy in empire)
            {
                UpdateEnemyResources(enemy);
                if (empireActs)
                {
                    ExecuteEmpireAction(enemy, null, techName, null, null);
                }
            }
        }
        
        await SaveEnemiesAsync();
        NotifyStateChanged();
    }
    
    // Triggered when player builds ships - enemies react with limited actions
    public async Task OnPlayerShipBuilt(string shipType, int quantity)
    {
        List<IGrouping<Guid, Enemy>> enemyEmpires;
        lock (_lockObject)
        {
            enemyEmpires = _enemies.Values.GroupBy(e => e.EmpireId).ToList();
        }

        foreach (var empire in enemyEmpires)
        {
            foreach (var enemy in empire)
            {
                UpdateEnemyResources(enemy);
                ExecuteEmpireAction(enemy, null, null, shipType, null);
            }
        }
        
        await SaveEnemiesAsync();
        NotifyStateChanged();
    }
    
    // Triggered when player attacks - enemies build defenses or spend resources
    public async Task OnPlayerAttack(int targetGalaxy, int targetSystem, int targetPosition, bool wasVictory)
    {
        var key = $"{targetGalaxy}:{targetSystem}:{targetPosition}";
        
        lock (_lockObject)
        {
            // Find the target enemy
            if (_enemies.TryGetValue(key, out var targetEnemy))
            {
                var empirePlanets = GetEmpirePlanets(targetEnemy);
                foreach (var planet in empirePlanets)
                {
                    ExecuteAttackReaction(planet);
                }
            }
            
            // Other enemies may also react to the attack (fear factor)
            foreach (var enemy in _enemies.Values)
            {
                if (enemy.Coordinates != key && _random.NextDouble() < 0.3) // 30% of other enemies react
                {
                    // First update their resources
                    ExecuteAttackReaction(enemy);
                }
            }
        }
        
        await SaveEnemiesAsync();
        NotifyStateChanged();
    }
    
    // Triggered when player builds defenses - enemies react with limited actions
    public async Task OnPlayerDefenseBuilt(string defenseType, int quantity)
    {
        List<IGrouping<Guid, Enemy>> enemyEmpires;
        lock (_lockObject)
        {
            enemyEmpires = _enemies.Values.GroupBy(e => e.EmpireId).ToList();
        }

        foreach (var empire in enemyEmpires)
        {
            bool empireActs = _random.NextDouble() < DEFENSE_BUILD_CHANCE;
            foreach (var enemy in empire)
            {
                UpdateEnemyResources(enemy);
                if (empireActs)
                {
                    ExecuteEmpireAction(enemy, null, null, null, defenseType);
                }
            }
        }
        
        await SaveEnemiesAsync();
        NotifyStateChanged();
    }

    private bool TryExploreGalaxy(Enemy enemy)
    {
        int galaxy = _random.Next(1, GalaxyService.MaxGalaxies + 1);
        int system = _random.Next(1, GalaxyService.MaxSystemsPerGalaxy + 1);
        enemy.ExploredGalaxies.Add(galaxy);

        var planets = _galaxyService.GetSystem(galaxy, system)
            .Where(p => p.IsOccupied && !(p.Galaxy == enemy.Galaxy && p.System == enemy.System && p.Position == enemy.Position))
            .ToList();
        if (!planets.Any()) return false;

        var discoveredPlanet = planets[_random.Next(planets.Count)];
        if (_random.NextDouble() > EXPLORE_DISCOVERY_CHANCE) return true;

        string discoveredCoordinates = $"{discoveredPlanet.Galaxy}:{discoveredPlanet.System}:{discoveredPlanet.Position}";
        AddKnownEnemyCoordinate(enemy, discoveredCoordinates);
        return true;
    }

    private void AddKnownEnemyCoordinate(Enemy enemy, string coordinates)
    {
        if (enemy.KnownEnemyCoordinates.Contains(coordinates)) return;

        enemy.KnownEnemyCoordinates.Add(coordinates);
        if (enemy.KnownEnemyCoordinates.Count > MAX_KNOWN_ENEMIES_MEMORY)
        {
            string removed = enemy.KnownEnemyCoordinates[0];
            enemy.KnownEnemyCoordinates.RemoveAt(0);
            enemy.SpiedEnemyPower.Remove(removed);
        }
    }

    private bool TrySpyKnownEnemy(Enemy enemy)
    {
        if (!enemy.KnownEnemyCoordinates.Any()) return false;

        string targetCoordinates = enemy.KnownEnemyCoordinates[_random.Next(enemy.KnownEnemyCoordinates.Count)];
        long targetPower = EstimateTargetPower(targetCoordinates);
        enemy.SpiedEnemyPower[targetCoordinates] = targetPower;
        return true;
    }

    private bool TryLaunchAttack(Enemy enemy)
    {
        if (!enemy.SpiedEnemyPower.Any()) return false;

        long attackerPower = CalculateEnemyPower(enemy);
        if (attackerPower <= 0) return false;

        var viableTargets = enemy.SpiedEnemyPower
            .Where(t => attackerPower >= t.Value * ATTACK_POWER_ADVANTAGE)
            .Select(t => t.Key)
            .ToList();
        if (!viableTargets.Any()) return false;

        var attackShips = enemy.Ships
            .Where(s => s.Value > 0)
            .ToDictionary(s => s.Key, s => s.Value);
        if (!attackShips.Any()) return false;

        string targetCoordinates = viableTargets[_random.Next(viableTargets.Count)];
        OnEnemyAttackLaunched?.Invoke(enemy.Coordinates, targetCoordinates, attackShips);
        enemy.LastActivity = DateTime.Now;
        return true;
    }

    private long EstimateTargetPower(string coordinates)
    {
        if (!TryParseCoordinates(coordinates, out int g, out int s, out int p)) return 1;

        lock (_lockObject)
        {
            if (_enemies.TryGetValue(coordinates, out var knownEnemy))
            {
                return CalculateEnemyPower(knownEnemy);
            }
        }

        var planet = _galaxyService.GetPlanet(g, s, p);
        if (planet == null || !planet.IsOccupied) return 1;

        // Unknown planets are treated as moderate threat until spied repeatedly.
        return 5000;
    }

    private static bool TryParseCoordinates(string coordinates, out int galaxy, out int system, out int position)
    {
        galaxy = 0;
        system = 0;
        position = 0;

        var parts = coordinates.Split(':');
        return parts.Length == 3 &&
               int.TryParse(parts[0], out galaxy) &&
               int.TryParse(parts[1], out system) &&
               int.TryParse(parts[2], out position);
    }

    private long CalculateEnemyPower(Enemy enemy)
    {
        long shipPower = 0;
        foreach (var ship in enemy.Ships)
        {
            var (metal, crystal, deuterium) = GetShipBaseCost(ship.Key);
            shipPower += (metal + crystal + deuterium) * ship.Value;
        }

        long defensePower = 0;
        foreach (var defense in enemy.Defenses)
        {
            var (metal, crystal, deuterium) = GetDefenseBaseCost(defense.Key);
            defensePower += (metal + crystal + deuterium) * defense.Value;
        }

        return shipPower + defensePower;
    }
    
    private bool TryUpgradeBuilding(Enemy enemy, string buildingName)
    {
        // First check if enemy meets requirements for this building
        if (!CanEnemyUpgradeBuilding(enemy, buildingName))
            return false;
        
        if (!enemy.Buildings.ContainsKey(buildingName))
            enemy.Buildings[buildingName] = 0;
        
        int currentLevel = enemy.Buildings[buildingName];
        
        // Calculate cost (simplified formula)
        double scaling = 2.0;
        long metalCost = (long)(GetBuildingBaseCost(buildingName, "Metal") * Math.Pow(scaling, currentLevel));
        long crystalCost = (long)(GetBuildingBaseCost(buildingName, "Crystal") * Math.Pow(scaling, currentLevel));
        long deuteriumCost = (long)(GetBuildingBaseCost(buildingName, "Deuterium") * Math.Pow(scaling, currentLevel));
        
        // Check if enemy has enough resources
        if (enemy.Metal >= metalCost && enemy.Crystal >= crystalCost && enemy.Deuterium >= deuteriumCost)
        {
            // Deduct resources
            enemy.Metal -= metalCost;
            enemy.Crystal -= crystalCost;
            enemy.Deuterium -= deuteriumCost;
            
            // Upgrade building
            enemy.Buildings[buildingName] = currentLevel + 1;
            return true;
        }
        
        return false;
    }
    
    private bool TryResearchTechnology(Enemy enemy, string techName)
    {
        // First check if enemy meets requirements for this technology
        if (!CanEnemyResearchTechnology(enemy, techName))
            return false;
        
        if (!enemy.Technologies.ContainsKey(techName))
            enemy.Technologies[techName] = 0;
        
        int currentLevel = enemy.Technologies[techName];
        
        // Max level check
        if (currentLevel >= 20) return false;
        
        // Calculate cost
        double scaling = 2.0;
        long metalCost = (long)(GetTechBaseCost(techName, "Metal") * Math.Pow(scaling, currentLevel));
        long crystalCost = (long)(GetTechBaseCost(techName, "Crystal") * Math.Pow(scaling, currentLevel));
        long deuteriumCost = (long)(GetTechBaseCost(techName, "Deuterium") * Math.Pow(scaling, currentLevel));
        
        // Check if enemy has enough resources
        if (enemy.Metal >= metalCost && enemy.Crystal >= crystalCost && enemy.Deuterium >= deuteriumCost)
        {
            // Deduct resources
            enemy.Metal -= metalCost;
            enemy.Crystal -= crystalCost;
            enemy.Deuterium -= deuteriumCost;
            
            // Research technology
            enemy.Technologies[techName] = currentLevel + 1;
            return true;
        }
        
        return false;
    }
    
    private bool TryBuildDefense(Enemy enemy, string defenseType)
    {
        // First check if enemy meets requirements for this defense
        if (!CanEnemyBuildDefense(enemy, defenseType))
            return false;
        
        // Get costs
        var (metalCost, crystalCost, deuteriumCost) = GetDefenseBaseCost(defenseType);
        
        // Check if enemy has enough resources
        if (enemy.Metal >= metalCost && enemy.Crystal >= crystalCost && enemy.Deuterium >= deuteriumCost)
        {
            // Deduct resources
            enemy.Metal -= metalCost;
            enemy.Crystal -= crystalCost;
            enemy.Deuterium -= deuteriumCost;
            
            // Build defense
            if (!enemy.Defenses.ContainsKey(defenseType))
                enemy.Defenses[defenseType] = 0;
            enemy.Defenses[defenseType]++;
            return true;
        }
        
        return false;
    }
    
    private bool TryBuildShip(Enemy enemy, string shipType)
    {
        // First check if enemy meets requirements for this ship
        if (!CanEnemyBuildShip(enemy, shipType))
            return false;
        
        // Get costs
        var (metalCost, crystalCost, deuteriumCost) = GetShipBaseCost(shipType);
        
        // Check if enemy has enough resources
        if (enemy.Metal >= metalCost && enemy.Crystal >= crystalCost && enemy.Deuterium >= deuteriumCost)
        {
            // Deduct resources
            enemy.Metal -= metalCost;
            enemy.Crystal -= crystalCost;
            enemy.Deuterium -= deuteriumCost;
            
            // Build ship
            if (!enemy.Ships.ContainsKey(shipType))
                enemy.Ships[shipType] = 0;
            enemy.Ships[shipType]++;
            return true;
        }
        
        return false;
    }
    
    private long GetBuildingBaseCost(string buildingName, string resourceType)
    {
        // Simplified base costs
        return buildingName switch
        {
            "Metal Mine" => resourceType == "Metal" ? 60 : resourceType == "Crystal" ? 15 : 0,
            "Crystal Mine" => resourceType == "Metal" ? 48 : resourceType == "Crystal" ? 24 : 0,
            "Deuterium Synthesizer" => resourceType == "Metal" ? 225 : resourceType == "Crystal" ? 75 : 0,
            "Solar Plant" => resourceType == "Metal" ? 75 : resourceType == "Crystal" ? 30 : 0,
            "Robotics Factory" => resourceType == "Metal" ? 400 : resourceType == "Crystal" ? 120 : resourceType == "Deuterium" ? 200 : 0,
            "Shipyard" => resourceType == "Metal" ? 400 : resourceType == "Crystal" ? 200 : resourceType == "Deuterium" ? 100 : 0,
            "Research Lab" => resourceType == "Metal" ? 200 : resourceType == "Crystal" ? 400 : resourceType == "Deuterium" ? 200 : 0,
            "Metal Storage" => resourceType == "Metal" ? 1000 : 0,
            "Crystal Storage" => resourceType == "Metal" ? 1000 : resourceType == "Crystal" ? 500 : 0,
            "Deuterium Tank" => resourceType == "Metal" ? 1000 : resourceType == "Crystal" ? 1000 : 0,
            _ => resourceType == "Metal" ? 500 : resourceType == "Crystal" ? 250 : 0
        };
    }
    
    private long GetTechBaseCost(string techName, string resourceType)
    {
        // Simplified base costs
        return techName switch
        {
            "Espionage Technology" => resourceType == "Metal" ? 200 : resourceType == "Crystal" ? 1000 : resourceType == "Deuterium" ? 200 : 0,
            "Computer Technology" => resourceType == "Crystal" ? 400 : resourceType == "Deuterium" ? 600 : 0,
            "Weapons Technology" => resourceType == "Metal" ? 800 : resourceType == "Crystal" ? 200 : 0,
            "Shielding Technology" => resourceType == "Metal" ? 200 : resourceType == "Crystal" ? 600 : 0,
            "Armour Technology" => resourceType == "Metal" ? 1000 : 0,
            "Energy Technology" => resourceType == "Crystal" ? 800 : resourceType == "Deuterium" ? 400 : 0,
            "Hyperspace Technology" => resourceType == "Crystal" ? 4000 : resourceType == "Deuterium" ? 2000 : 0,
            "Combustion Drive" => resourceType == "Metal" ? 400 : resourceType == "Deuterium" ? 600 : 0,
            "Impulse Drive" => resourceType == "Metal" ? 2000 : resourceType == "Crystal" ? 4000 : resourceType == "Deuterium" ? 600 : 0,
            "Hyperspace Drive" => resourceType == "Metal" ? 10000 : resourceType == "Crystal" ? 20000 : resourceType == "Deuterium" ? 6000 : 0,
            "Laser Technology" => resourceType == "Metal" ? 200 : resourceType == "Crystal" ? 100 : 0,
            "Ion Technology" => resourceType == "Metal" ? 1000 : resourceType == "Crystal" ? 300 : resourceType == "Deuterium" ? 100 : 0,
            "Plasma Technology" => resourceType == "Metal" ? 2000 : resourceType == "Crystal" ? 4000 : resourceType == "Deuterium" ? 1000 : 0,
            _ => resourceType == "Metal" ? 500 : resourceType == "Crystal" ? 250 : 0
        };
    }
    
    private (long metal, long crystal, long deuterium) GetDefenseBaseCost(string defenseType)
    {
        return defenseType switch
        {
            "Rocket Launcher" => (2000, 0, 0),
            "Light Laser" => (1500, 500, 0),
            "Heavy Laser" => (6000, 2000, 0),
            "Gauss Cannon" => (20000, 15000, 2000),
            "Ion Cannon" => (2000, 6000, 0),
            "Plasma Turret" => (50000, 50000, 30000),
            "Small Shield Dome" => (10000, 10000, 0),
            "Large Shield Dome" => (50000, 50000, 0),
            "Anti-Ballistic Missile" => (8000, 0, 2000),
            _ => (1000, 500, 0)
        };
    }
    
    private (long metal, long crystal, long deuterium) GetShipBaseCost(string shipType)
    {
        return shipType switch
        {
            "SC" => (2000, 2000, 0),
            "LC" => (6000, 6000, 0),
            "LF" => (3000, 1000, 0),
            "HF" => (6000, 4000, 0),
            "CR" => (20000, 7000, 2000),
            "BS" => (45000, 15000, 0),
            "CS" => (10000, 20000, 10000),
            "REC" => (10000, 6000, 2000),
            "ESP" => (0, 1000, 0),
            "DST" => (60000, 50000, 15000),
            "RIP" => (5000000, 4000000, 1000000),
            _ => (5000, 2000, 0)
        };
    }
    
    public Enemy? GetEnemy(int galaxy, int system, int position)
    {
        var key = $"{galaxy}:{system}:{position}";
        lock (_lockObject)
        {
            return _enemies.TryGetValue(key, out var enemy) ? enemy : null;
        }
    }
    
    public List<Enemy> GetEnemiesInSystem(int galaxy, int system)
    {
        lock (_lockObject)
        {
            return _enemies.Values.Where(e => e.Galaxy == galaxy && e.System == system).ToList();
        }
    }
    
    public async Task ResetEnemies()
    {
        lock (_lockObject)
        {
            _enemies.Clear();
        }
        
        // Clear database
        var allEnemies = await _dbContext.Enemies.ToListAsync();
        _dbContext.Enemies.RemoveRange(allEnemies);
        await _dbContext.SaveChangesAsync();
        
        // Generate new enemies using current player location
        _isInitialized = false;
        await GenerateInitialEnemiesAsync(
            _galaxyService.HomeGalaxy,
            _galaxyService.HomeSystem,
            _galaxyService.HomePosition
        );
    }
    
    private void NotifyStateChanged() => OnChange?.Invoke();
}
