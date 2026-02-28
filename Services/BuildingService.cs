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

public class BuildingService
{
    private readonly GameDbContext _dbContext;
    private readonly ResourceService _resourceService;
    private readonly DevModeService _devModeService;
    private readonly EnemyService _enemyService;
    private readonly PlayerStateService _playerStateService;
    public List<Building> Buildings { get; private set; } = new();
    public List<Building> ConstructionQueue { get; private set; } = new();
    
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

    public BuildingService(GameDbContext dbContext, ResourceService resourceService, DevModeService devModeService, EnemyService enemyService, PlayerStateService playerStateService)
    {
        _dbContext = dbContext;
        _resourceService = resourceService;
        _devModeService = devModeService;
        _enemyService = enemyService;
        _playerStateService = playerStateService;
        
        InitializeBuildings();
        
        _playerStateService.OnChange += async () => 
        {
            await LoadFromDatabaseAsync();
            await UpdateProductionAsync();
            NotifyStateChanged();
        };
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        
        await LoadFromDatabaseAsync();
        await UpdateProductionAsync();
        
        _isInitialized = true;
    }

    public void ResetState()
    {
        Buildings.Clear();
        ConstructionQueue.Clear();
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
        }
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
        // Important: Update resources before spending them!
        await UpdateProductionAsync();

        if (ConstructionQueue.Count >= MaxQueueSize) return;
        
        if (await _resourceService.HasResourcesAsync(building.MetalCost, building.CrystalCost, building.DeuteriumCost))
        {
            await _resourceService.ConsumeResourcesAsync(building.MetalCost, building.CrystalCost, building.DeuteriumCost);
            
            var calculatedDuration = CalculateConstructionDuration(building);
            var duration = _devModeService.GetDuration(calculatedDuration, 1);
            building.ConstructionDuration = duration;
            building.TimeRemaining = duration;
            
            ConstructionQueue.Add(building);
            NotifyStateChanged();

            if (!_isProcessingQueue)
            {
                _ = ProcessQueue();
            }
        }
    }

    public async Task CancelAsync(Building building)
    {
        if (ConstructionQueue.Contains(building))
        {
            // Refund resources based on CancelRefundPercentage setting
            await _resourceService.RefundResourcesAsync(building.MetalCost, building.CrystalCost, building.DeuteriumCost);
            
            ConstructionQueue.Remove(building);
            building.IsBuilding = false;
            building.TimeRemaining = TimeSpan.Zero;
            NotifyStateChanged();
        }
    }

    private async Task ProcessQueue()
    {
        _isProcessingQueue = true;

        while (ConstructionQueue.Count > 0)
        {
            var currentBuilding = ConstructionQueue[0];
            currentBuilding.IsBuilding = true;
            NotifyStateChanged();

            while (currentBuilding.TimeRemaining > TimeSpan.Zero)
            {
                // Check if building was cancelled
                if(!ConstructionQueue.Contains(currentBuilding)) break;

                await Task.Delay(1000);
                currentBuilding.TimeRemaining = currentBuilding.TimeRemaining.Subtract(TimeSpan.FromSeconds(1));
                NotifyStateChanged();
            }

            // Check again if cancelled
            if(!ConstructionQueue.Contains(currentBuilding)) continue;

            // Upgrade Logic
            currentBuilding.IsBuilding = false;

            // CRITICAL: Update production resources based on OLD levels before upgrading
            // This ensures we don't calculate the past duration with the NEW production rate
            await UpdateProductionAsync();
            
            // Energy update is now handled INSIDE UpdateProduction(), so we don't need manual logic here anymore.
            // Just increment level and recalculate.

            string upgradedBuildingName = currentBuilding.Title;
            currentBuilding.Level++;
            
            // Save to database
            await SaveToDatabaseAsync();
            
            ConstructionQueue.RemoveAt(0);
            
            // Recalculate production rates (and energy) with new levels
            await UpdateProductionAsync();
            
            // Notify enemy service that player upgraded a building
            _ = _enemyService.OnPlayerBuildingUpgraded(upgradedBuildingName);
            
            NotifyStateChanged();
        }

        _isProcessingQueue = false;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public int GetBuildingLevel(string buildingName)
    {
        var building = Buildings.FirstOrDefault(b => b.Title == buildingName);
        return building?.Level ?? 0;
    }
}
