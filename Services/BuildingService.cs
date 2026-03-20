using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

// Refer to wiki/business-rules/Buildings.md for business rules documentation

public class Building
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Image { get; set; } = "";
    public long BaseMetalCost { get; set; }
    public long BaseCrystalCost { get; set; }
    public long BaseDeuteriumCost { get; set; }
    public TimeSpan BaseDuration { get; set; }
    public int EnergyConsumption { get; set; }
    public int Level { get; set; } = 0; // Default level 0 so we can build level 1
    public double Scaling { get; set; } = 2.0;
    public List<string> Dependencies { get; set; } = new();
    
    // Runtime state
    public TimeSpan ConstructionDuration { get; set; }
    public bool IsBuilding { get; set; }
    public TimeSpan TimeRemaining { get; set; }

    // Calculated properties
    public long MetalCost => (long)(BaseMetalCost * Math.Pow(Scaling, Level));
    public long CrystalCost => (long)(BaseCrystalCost * Math.Pow(Scaling, Level));
    public long DeuteriumCost => (long)(BaseDeuteriumCost * Math.Pow(Scaling, Level));
    
    // Duration calculated by BuildingService.CalculateConstructionDuration()
    public TimeSpan Duration { get; set; }
}

public class QueuedBuildingState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public int LevelBeforeUpgrade { get; set; }
    public TimeSpan ConstructionDuration { get; set; }
    public TimeSpan TimeRemaining { get; set; }
    public bool IsBuilding { get; set; }
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }
}

public class BuildingService
{
    private readonly GameDbContext _dbContext;
    private readonly ResourceService _resourceService;
    private readonly DevModeService _devModeService;
    private readonly EnemyService _enemyService;
    private readonly PlayerStateService _playerStateService;
    private readonly RankingService _rankingService;
    public List<Building> Buildings { get; private set; } = new();
    public List<Building> ConstructionQueue { get; private set; } = new();
    private List<QueuedBuildingState> _allConstructionQueue = new();
    
    public event Action? OnChange;

    private bool _isProcessingQueue = false;

    // Public properties for UI display (Tooltips/Stats)
    public double MetalHourlyProduction { get; private set; }
    public double CrystalHourlyProduction { get; private set; }
    public double DeuteriumHourlyProduction { get; private set; }
    
    public long MetalMineEnergyConsumption { get; private set; }
    public long CrystalMineEnergyConsumption { get; private set; }
    public long DeuteriumSynthesizerEnergyConsumption { get; private set; }
    
    public double ProductionFactor { get; private set; } = 1.0;

private bool _isInitialized = false;
    private int _maxQueueSize = 5;

    public int MaxQueueSize
    {
        get => _maxQueueSize;
        set => _maxQueueSize = Math.Max(1, value);
    }

    public BuildingService(GameDbContext dbContext, ResourceService resourceService, DevModeService devModeService, EnemyService enemyService, PlayerStateService playerStateService, RankingService? rankingService = null)
    {
        _dbContext = dbContext;
        _resourceService = resourceService;
        _devModeService = devModeService;
        _enemyService = enemyService;
        _playerStateService = playerStateService;
        _rankingService = rankingService;
        
        InitializeBuildings();
        _ = ProcessQueueLoop();
        
        _playerStateService.OnChange += async () => 
        {
            await LoadFromDatabaseAsync();
            await LoadQueueFromDatabaseAsync();
            await UpdateProductionAsync();
            SyncVisibleConstructionQueue();
            NotifyStateChanged();
        };
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        
        await LoadFromDatabaseAsync();
        await LoadQueueFromDatabaseAsync();
        await UpdateProductionAsync();
        SyncVisibleConstructionQueue();
        
        _isInitialized = true;
    }

    public void ResetState()
    {
        Buildings.Clear();
        ConstructionQueue.Clear();
        _allConstructionQueue.Clear();
        MetalHourlyProduction = 0;
        CrystalHourlyProduction = 0;
        DeuteriumHourlyProduction = 0;
        MetalMineEnergyConsumption = 0;
        CrystalMineEnergyConsumption = 0;
        DeuteriumSynthesizerEnergyConsumption = 0;
        ProductionFactor = 1.0;
        _isInitialized = false;
        _isProcessingQueue = false;
        InitializeBuildings();
        SyncVisibleConstructionQueue();
    }

    private async Task LoadFromDatabaseAsync()
    {
        int g = _playerStateService.ActiveGalaxy;
        int s = _playerStateService.ActiveSystem;
        int p = _playerStateService.ActivePosition;

        var dbBuildings = await _dbContext.Buildings
            .Where(b => b.Galaxy == g && b.System == s && b.Position == p)
            .ToListAsync();

        foreach (var building in Buildings)
        {
            var dbBuilding = dbBuildings.FirstOrDefault(b => b.BuildingType == building.Title);
            if (dbBuilding != null)
            {
                building.Level = dbBuilding.Level;
            }
            else
            {
                building.Level = 0;
            }

            building.IsBuilding = false;
            building.TimeRemaining = TimeSpan.Zero;
            building.ConstructionDuration = TimeSpan.Zero;
        }

        SyncVisibleConstructionQueue();
    }

    private async Task LoadQueueFromDatabaseAsync()
    {
        var dbQueue = await _dbContext.BuildingQueue
            .OrderBy(q => q.Galaxy)
            .ThenBy(q => q.System)
            .ThenBy(q => q.Position)
            .ThenBy(q => q.QueuePosition)
            .ThenBy(q => q.StartTime)
            .ToListAsync();

        _allConstructionQueue = dbQueue
            .Select(q => new QueuedBuildingState
            {
                Id = q.Id,
                Title = q.BuildingType,
                LevelBeforeUpgrade = q.TargetLevel,
                ConstructionDuration = q.Duration,
                TimeRemaining = q.IsCompleted ? TimeSpan.Zero : q.TimeRemaining,
                IsBuilding = q.IsProcessing,
                Galaxy = q.Galaxy,
                System = q.System,
                Position = q.Position
            })
            .ToList();

        SyncVisibleConstructionQueue();
    }

    private async Task SaveToDatabaseAsync()
    {
        int g = _playerStateService.ActiveGalaxy;
        int s = _playerStateService.ActiveSystem;
        int p = _playerStateService.ActivePosition;

        foreach (var building in Buildings)
        {
            var dbBuilding = await _dbContext.Buildings.FirstOrDefaultAsync(b => 
                b.BuildingType == building.Title && b.Galaxy == g && b.System == s && b.Position == p);
            
            if (dbBuilding != null)
            {
                dbBuilding.Level = building.Level;
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateProductionAsync()
    {
        // Configuration Multiplier (x1000 speed for testing/fast servers)
        double speedMultiplier = 1000.0;

        double metalProduction = 0;
        double crystalProduction = 0;
        double deuteriumProduction = 0;

        long energyProduction = 0;
        long energyConsumption = 0;

        // 1. Calculate Energy First
        var solarPlant = Buildings.FirstOrDefault(b => b.Title == "Solar Plant");
        if (solarPlant != null && solarPlant.Level > 0)
        {
            // Solar Plant Formula: 20 * Level * 1.1^Level
            energyProduction = (long)(20 * solarPlant.Level * Math.Pow(1.1, solarPlant.Level));
        }

        // Calculate Consumption (Mines)
        var metalMine = Buildings.FirstOrDefault(b => b.Title == "Metal Mine");
        if (metalMine != null && metalMine.Level > 0)
        {
            energyConsumption += (long)(10 * metalMine.Level * Math.Pow(1.1, metalMine.Level));
        }
        
        var crystalMine = Buildings.FirstOrDefault(b => b.Title == "Crystal Mine");
        if (crystalMine != null && crystalMine.Level > 0)
        {
             energyConsumption += (long)(10 * crystalMine.Level * Math.Pow(1.1, crystalMine.Level));
        }
        
        var deuteriumSynthesizer = Buildings.FirstOrDefault(b => b.Title == "Deuterium Synthesizer");
        if (deuteriumSynthesizer != null && deuteriumSynthesizer.Level > 0)
        {
             energyConsumption += (long)(20 * deuteriumSynthesizer.Level * Math.Pow(1.1, deuteriumSynthesizer.Level));
        }

        // Update Energy State
        long netEnergy = energyProduction - energyConsumption;
        await _resourceService.SetEnergyAsync(netEnergy);

        // 2. Calculate Production Factor based on Energy
        ProductionFactor = 1.0;
        if (netEnergy < 0)
        {
             // If net energy is negative, production drops.
             // Formula: Production = FullProduction * (AvailableEnergy / NeededEnergy)
             // AvailableEnergy = Produced Energy (energyProduction)
             // NeededEnergy = Consumption (energyConsumption)
             if (energyConsumption > 0) 
             {
                ProductionFactor = (double)energyProduction / (double)energyConsumption;
             }
             else 
             {
                ProductionFactor = 0;
             }
        }
        
        // 3. Calculate Resource Production
        if (metalMine != null && metalMine.Level > 0)
        {
            // Example: Level 1 = 30/hr = 0.0083/sec
            double rawMetal = (30 * metalMine.Level * Math.Pow(1.1, metalMine.Level));
            // Store for UI display (Apply multiplier for "Speed Universe" feel, or display base?)
            // Usually OGame displays the ACTUAL production including speed.
            MetalHourlyProduction = rawMetal * ProductionFactor * speedMultiplier;
            
            // Calculate consumption for specific mine for UI
            MetalMineEnergyConsumption = (long)(10 * metalMine.Level * Math.Pow(1.1, metalMine.Level));

            metalProduction = (rawMetal * ProductionFactor * speedMultiplier) / 3600.0;
        }
        else 
        {
             MetalHourlyProduction = 0;
             MetalMineEnergyConsumption = 0;
        }

        if (crystalMine != null && crystalMine.Level > 0)
        {
            // Example: Level 1 = 20/hr
            double rawCrystal = (20 * crystalMine.Level * Math.Pow(1.1, crystalMine.Level));
            CrystalHourlyProduction = rawCrystal * ProductionFactor * speedMultiplier;

            CrystalMineEnergyConsumption = (long)(10 * crystalMine.Level * Math.Pow(1.1, crystalMine.Level));

            crystalProduction = (rawCrystal * ProductionFactor * speedMultiplier) / 3600.0;
        }
        else
        {
            CrystalHourlyProduction = 0;
            CrystalMineEnergyConsumption = 0;
        }

        if (deuteriumSynthesizer != null && deuteriumSynthesizer.Level > 0)
        {
            // Example: Level 1 = 10/hr
            double rawDeuterium = (10 * deuteriumSynthesizer.Level * Math.Pow(1.1, deuteriumSynthesizer.Level));
            DeuteriumHourlyProduction = rawDeuterium * ProductionFactor * speedMultiplier;
            
            DeuteriumSynthesizerEnergyConsumption = (long)(20 * deuteriumSynthesizer.Level * Math.Pow(1.1, deuteriumSynthesizer.Level));

            deuteriumProduction = (rawDeuterium * ProductionFactor * speedMultiplier) / 3600.0;
        }
        else
        {
            DeuteriumHourlyProduction = 0;
            DeuteriumSynthesizerEnergyConsumption = 0;
        }
        
        // Base production (always on even with 0 mines) - NOT affected by energy or multiplier usually, 
        // but applying multiplier for consistency if desired. Usually base is base.
        // Let's apply multiplier to base too for fast start.
        double baseMetalProd = 30.0 * speedMultiplier;
        double baseCrystalProd = 15.0 * speedMultiplier;

        MetalHourlyProduction += baseMetalProd;
        CrystalHourlyProduction += baseCrystalProd;
        
        metalProduction += baseMetalProd / 3600.0;
        crystalProduction += baseCrystalProd / 3600.0;

        _resourceService.MetalProductionRate = metalProduction;
        _resourceService.CrystalProductionRate = crystalProduction;
        _resourceService.DeuteriumProductionRate = deuteriumProduction;
    }

    public TimeSpan CalculateConstructionDuration(Building building)
    {
        int roboticsLevel = GetBuildingLevel("Robotics Factory");
        int naniteLevel = GetBuildingLevel("Nanite Factory");
        
        long metalCost = building.MetalCost;
        long crystalCost = building.CrystalCost;
        
        // Formula: Time(hours) = (Metal + Crystal) / (2500 * (1 + Robotics) * 2^Nanite * UniverseSpeed)
        double universeSpeed = 1.0;
        double divisor = 2500.0 * (1.0 + roboticsLevel) * Math.Pow(2, naniteLevel) * universeSpeed;
        
        double hours = (metalCost + crystalCost) / divisor;
        double seconds = hours * 3600.0;
        
        return TimeSpan.FromSeconds(seconds);
    }

    public TimeSpan GetConstructionTime(Building building)
    {
        var calculatedDuration = CalculateConstructionDuration(building);
        return _devModeService.GetDuration(calculatedDuration, 1);
    }

    private void InitializeBuildings()
    {
         Buildings.Add(new Building
        {
            Title = "Metal Mine",
            Description = "Produces Metal.",
            BaseMetalCost = 60,
            BaseCrystalCost = 15,
            BaseDeuteriumCost = 0,
            ConstructionDuration = TimeSpan.FromSeconds(10),
             Image = "assets/buildings/building1.jpg",
            EnergyConsumption = 10
        });
        Buildings.Add(new Building
        {
            Title = "Crystal Mine",
            Description = "Produces Crystal.",
            BaseMetalCost = 48,
            BaseCrystalCost = 24,
            BaseDeuteriumCost = 0,
            ConstructionDuration = TimeSpan.FromSeconds(15),
             Image = "assets/buildings/building2.jpg",
            EnergyConsumption = 8
        });
        Buildings.Add(new Building
        {
            Title = "Deuterium Synthesizer",
            Description = "Produces Deuterium.",
            BaseMetalCost = 225,
            BaseCrystalCost = 75,
            BaseDeuteriumCost = 0,   
            ConstructionDuration = TimeSpan.Parse("00:00:30"),
             Image = "assets/buildings/building3.jpg",
            EnergyConsumption = 15
        });
        Buildings.Add(new Building
        {
            Title = "Solar Plant",
            Description = "Generates Energy.",
            BaseMetalCost = 75,
            BaseCrystalCost = 30,
            BaseDeuteriumCost = 0,    
            ConstructionDuration = TimeSpan.Parse("00:00:20"),
             Image = "assets/buildings/building4.jpg",
            EnergyConsumption = -20
        });
        Buildings.Add(new Building
        {
            Title = "Robotics Factory",
            Description = "Builds robots.",
            BaseMetalCost = 400,
            BaseCrystalCost = 120,
            BaseDeuteriumCost = 200,    
            ConstructionDuration = TimeSpan.Parse("00:01:00"),
             Image = "assets/buildings/building5.jpg",
            EnergyConsumption = 30
        });

        Buildings.Add(new Building
        {
            Title = "Fusion Reactor",
            Description = "Produces Energy by consuming Deuterium.",
            BaseMetalCost = 900,
            BaseCrystalCost = 360,
            BaseDeuteriumCost = 180,    
            ConstructionDuration = TimeSpan.Parse("00:00:45"),
             Image = "assets/buildings/building14.jpg",
            EnergyConsumption = 0 // Special handling: Produces Energy, Consumes Deuterium
        });

        Buildings.Add(new Building
        {
            Title = "Alliance Depot",
            Description = "Supplies fuel to friendly fleets.",
            BaseMetalCost = 20000,
            BaseCrystalCost = 40000,
            BaseDeuteriumCost = 0,    
            ConstructionDuration = TimeSpan.Parse("00:10:00"),
             Image = "assets/buildings/building15.jpg",
            EnergyConsumption = 0
        });

        Buildings.Add(new Building
        {
            Title = "Shipyard",
            Description = "Builds ships.",
            BaseMetalCost = 400,
            BaseCrystalCost = 200,
            BaseDeuteriumCost = 100,    
            ConstructionDuration = TimeSpan.Parse("00:01:30"),
             Image = "assets/buildings/building6.jpg",
            EnergyConsumption = 0 // Shipyard does not consume energy continuously in OGame logic usually, unless active? Let's keep 0 for base.
        });
        Buildings.Add(new Building
        {
            Title = "Metal Storage",
            Description = "Stores Metal.",
            BaseMetalCost = 1000, // Updated to OGame base
            BaseCrystalCost = 0,
            BaseDeuteriumCost = 0,    
            ConstructionDuration = TimeSpan.Parse("00:00:45"),
             Image = "assets/buildings/building7.jpg",
            EnergyConsumption = 0
        });
        Buildings.Add(new Building
        {
            Title = "Crystal Storage",
            Description = "Stores Crystal.",
            BaseMetalCost = 1000, // Updated
            BaseCrystalCost = 500, // Updated
            BaseDeuteriumCost = 0,    
            ConstructionDuration = TimeSpan.Parse("00:00:50"),
             Image = "assets/buildings/building8.jpg",
            EnergyConsumption = 0
        });
        Buildings.Add(new Building
        {
            Title = "Deuterium Tank", // Renamed from Storage to Tank usually
            Description = "Stores Deuterium.",
            BaseMetalCost = 1000, // Updated
            BaseCrystalCost = 1000, // Updated
            BaseDeuteriumCost = 0,    
            ConstructionDuration = TimeSpan.Parse("00:00:55"),
             Image = "assets/buildings/building9.jpg",
            EnergyConsumption = 0
        });
        Buildings.Add(new Building
        {
            Title = "Research Lab",
            Description = "Allows to do Research.",
            BaseMetalCost = 200, // Updated
            BaseCrystalCost = 400, // Updated
            BaseDeuteriumCost = 200,    
            ConstructionDuration = TimeSpan.Parse("00:01:15"),
             Image = "assets/buildings/building10.jpg",
            EnergyConsumption = 0 // Lab doesn't consume energy passively usually
        });

        Buildings.Add(new Building
        {
            Title = "Terraformer",
            Description = "Allows to build more buildings.",
            BaseMetalCost = 0,
            BaseCrystalCost = 50000, // Updated
            BaseDeuteriumCost = 100000, // Updated
            ConstructionDuration = TimeSpan.Parse("01:00:00"),
             Image = "assets/buildings/building11.jpg",
            EnergyConsumption = 0 // Consumes energy only for BUILD cost usually? Or passive? OGame Terraformer cost includes Energy!
            // Note: Building cost logic needs to handle Energy cost if we want to be strict.
        });

        Buildings.Add(new Building
        {
            Title = "Missile Silo",
            Description = "Stores interplantary missiles.",
            BaseMetalCost = 20000,
            BaseCrystalCost = 20000,
            BaseDeuteriumCost = 1000,    
            ConstructionDuration = TimeSpan.Parse("00:05:00"),
             Image = "assets/buildings/building12.jpg",
            EnergyConsumption = 0
        });
        Buildings.Add(new Building
        {
            Title = "Nanite Factory",
            Description = "Increases Building speed.",
            BaseMetalCost = 1000000,
            BaseCrystalCost = 500000,
            BaseDeuteriumCost = 100000,    
            ConstructionDuration = TimeSpan.Parse("02:00:00"),
             Image = "assets/buildings/building13.jpg",
            EnergyConsumption = 0 // Nanite doesn't consume energy
        });
    }

    public async Task AddToQueueAsync(Building building)
    {
        if (GetCurrentPlanetQueue().Count >= MaxQueueSize) return;

        int queuedSameBuilding = GetCurrentPlanetQueue().Count(q => q.Title == building.Title);
        var queueBuilding = CreateQueueDisplayBuilding(building.Title, building.Level + queuedSameBuilding);
        if (queueBuilding == null) return;

        if (await _resourceService.HasResourcesAsync(queueBuilding.MetalCost, queueBuilding.CrystalCost, queueBuilding.DeuteriumCost))
        {
            await _resourceService.ConsumeResourcesAsync(queueBuilding.MetalCost, queueBuilding.CrystalCost, queueBuilding.DeuteriumCost);
            _rankingService?.AddSpendingPoints(RankingService.PlayerKey, RankingService.PlayerName, false, queueBuilding.MetalCost, queueBuilding.CrystalCost, queueBuilding.DeuteriumCost);

            var calculatedDuration = CalculateConstructionDuration(queueBuilding);
            var duration = _devModeService.GetDuration(calculatedDuration, 1);
            int g = _playerStateService.ActiveGalaxy;
            int s = _playerStateService.ActiveSystem;
            int p = _playerStateService.ActivePosition;

            _allConstructionQueue.Add(new QueuedBuildingState
            {
                Title = building.Title,
                LevelBeforeUpgrade = queueBuilding.Level,
                ConstructionDuration = duration,
                TimeRemaining = duration,
                Galaxy = g,
                System = s,
                Position = p
            });

            await PersistQueueForPlanetAsync(g, s, p);
            SyncVisibleConstructionQueue();
            NotifyStateChanged();
        }
    }

    public async Task CancelAsync(Building building)
    {
        var queuedItem = GetCurrentPlanetQueue().FirstOrDefault(q => q.Title == building.Title && q.LevelBeforeUpgrade == building.Level);
        if (queuedItem == null) return;

        var queueBuilding = CreateQueueDisplayBuilding(queuedItem.Title, queuedItem.LevelBeforeUpgrade);
        if (queueBuilding == null) return;

        await _resourceService.RefundResourcesAsync(queueBuilding.MetalCost, queueBuilding.CrystalCost, queueBuilding.DeuteriumCost);
        _allConstructionQueue.Remove(queuedItem);
        await PersistQueueForPlanetAsync(queuedItem.Galaxy, queuedItem.System, queuedItem.Position);
        SyncVisibleConstructionQueue();
        NotifyStateChanged();
    }

    private async Task ProcessQueueLoop()
    {
        while (true)
        {
            foreach (var queueGroup in _allConstructionQueue.GroupBy(q => $"{q.Galaxy}:{q.System}:{q.Position}").ToList())
            {
                var currentItem = queueGroup.FirstOrDefault();
                if (currentItem == null) continue;

                currentItem.IsBuilding = true;
                currentItem.TimeRemaining = currentItem.TimeRemaining.Subtract(TimeSpan.FromSeconds(1));

                if (currentItem.TimeRemaining <= TimeSpan.Zero)
                {
                    currentItem.IsBuilding = false;
                    await IncrementBuildingLevelForPlanetAsync(currentItem.Title, currentItem.Galaxy, currentItem.System, currentItem.Position);
                    _allConstructionQueue.Remove(currentItem);
                    await PersistQueueForPlanetAsync(currentItem.Galaxy, currentItem.System, currentItem.Position);

                    if (IsCurrentPlanet(currentItem.Galaxy, currentItem.System, currentItem.Position))
                    {
                        await _resourceService.SettleActivePlanetResourcesAsync();
                        await LoadFromDatabaseAsync();
                        await UpdateProductionAsync();
                    }

                    _ = _enemyService.OnPlayerBuildingUpgraded(currentItem.Title);
                }
            }

            SyncVisibleConstructionQueue();
            NotifyStateChanged();
            await Task.Delay(1000);
        }
    }

    private void SyncVisibleConstructionQueue()
    {
        ConstructionQueue = GetCurrentPlanetQueue()
            .Select(q =>
            {
                var queueBuilding = CreateQueueDisplayBuilding(q.Title, q.LevelBeforeUpgrade);
                if (queueBuilding == null) return null;

                queueBuilding.ConstructionDuration = q.ConstructionDuration;
                queueBuilding.TimeRemaining = q.TimeRemaining;
                queueBuilding.IsBuilding = q.IsBuilding;
                return queueBuilding;
            })
            .Where(q => q != null)
            .Cast<Building>()
            .ToList();
    }

    private List<QueuedBuildingState> GetCurrentPlanetQueue()
    {
        return _allConstructionQueue
            .Where(q => IsCurrentPlanet(q.Galaxy, q.System, q.Position))
            .OrderBy(q => q.Id)
            .ToList();
    }

    private bool IsCurrentPlanet(int galaxy, int system, int position)
    {
        return galaxy == _playerStateService.ActiveGalaxy &&
               system == _playerStateService.ActiveSystem &&
               position == _playerStateService.ActivePosition;
    }

    private Building? CreateQueueDisplayBuilding(string title, int level)
    {
        var source = Buildings.FirstOrDefault(b => b.Title == title);
        if (source == null) return null;

        return new Building
        {
            Title = source.Title,
            Description = source.Description,
            Image = source.Image,
            BaseMetalCost = source.BaseMetalCost,
            BaseCrystalCost = source.BaseCrystalCost,
            BaseDeuteriumCost = source.BaseDeuteriumCost,
            BaseDuration = source.BaseDuration,
            EnergyConsumption = source.EnergyConsumption,
            Scaling = source.Scaling,
            Dependencies = new(source.Dependencies),
            Level = level
        };
    }

    public bool IsQueued(string buildingTitle)
    {
        return GetCurrentPlanetQueue().Any(q => q.Title == buildingTitle);
    }

    public Building? GetQueuedBuilding(string buildingTitle)
    {
        var queuedItem = GetCurrentPlanetQueue().FirstOrDefault(q => q.Title == buildingTitle);
        if (queuedItem == null) return null;

        var queueBuilding = CreateQueueDisplayBuilding(queuedItem.Title, queuedItem.LevelBeforeUpgrade);
        if (queueBuilding == null) return null;

        queueBuilding.ConstructionDuration = queuedItem.ConstructionDuration;
        queueBuilding.TimeRemaining = queuedItem.TimeRemaining;
        queueBuilding.IsBuilding = queuedItem.IsBuilding;
        return queueBuilding;
    }

    private async Task IncrementBuildingLevelForPlanetAsync(string buildingTitle, int g, int s, int p)
    {
        var dbBuilding = await _dbContext.Buildings.FirstOrDefaultAsync(b =>
            b.BuildingType == buildingTitle && b.Galaxy == g && b.System == s && b.Position == p);
        if (dbBuilding == null) return;

        dbBuilding.Level++;
        await _dbContext.SaveChangesAsync();
    }

    private async Task PersistQueueForPlanetAsync(int g, int s, int p)
    {
        var planetQueue = _allConstructionQueue
            .Where(q => q.Galaxy == g && q.System == s && q.Position == p)
            .OrderBy(q => q.Id)
            .ToList();

        var existing = await _dbContext.BuildingQueue
            .Where(q => q.Galaxy == g && q.System == s && q.Position == p)
            .ToListAsync();

        if (existing.Any())
        {
            _dbContext.BuildingQueue.RemoveRange(existing);
            await _dbContext.SaveChangesAsync();
        }

        if (!planetQueue.Any()) return;

        DateTime nextStart = DateTime.UtcNow;
        var entities = new List<BuildingQueueEntity>();

        for (int i = 0; i < planetQueue.Count; i++)
        {
            var item = planetQueue[i];
            item.IsBuilding = i == 0;
            item.TimeRemaining = i == 0 ? GetRemainingDuration(item.TimeRemaining, item.ConstructionDuration) : item.ConstructionDuration;

            DateTime startTime = nextStart;
            DateTime endTime = startTime.Add(item.TimeRemaining);
            nextStart = endTime;

            entities.Add(new BuildingQueueEntity
            {
                Id = item.Id,
                BuildingType = item.Title,
                TargetLevel = item.LevelBeforeUpgrade,
                Galaxy = g,
                System = s,
                Position = p,
                StartTime = startTime,
                EndTime = endTime,
                IsProcessing = item.IsBuilding,
                QueuePosition = i
            });
        }

        await _dbContext.BuildingQueue.AddRangeAsync(entities);
        await _dbContext.SaveChangesAsync();
    }

    private static TimeSpan GetRemainingDuration(TimeSpan remaining, TimeSpan fallback)
    {
        if (remaining <= TimeSpan.Zero) return fallback;
        if (remaining > fallback) return fallback;
        return remaining;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public int GetBuildingLevel(string buildingName)
    {
        var building = Buildings.FirstOrDefault(b => b.Title == buildingName);
        return building?.Level ?? 0;
    }
}



