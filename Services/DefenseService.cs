using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

public class DefenseUnit
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Image { get; set; } = "";
    
    // Costs
    public long MetalCost { get; set; }
    public long CrystalCost { get; set; }
    public long DeuteriumCost { get; set; }
    
    // Stats
    public long Structure { get; set; }
    public long Shield { get; set; }
    public long Attack { get; set; }
    
    // Construction
    public TimeSpan BaseDuration { get; set; }
    
    public Dictionary<string, int> Requirements { get; set; } = new();
}

public class DefenseQueueItem
{
    public DefenseUnit Unit { get; set; } = null!;
    public int Quantity { get; set; }
    public TimeSpan DurationPerUnit { get; set; }
    public TimeSpan TimeRemaining { get; set; }
}

public class DefenseService
{
    private readonly GameDbContext _dbContext;
    private readonly ResourceService _resourceService;
    private readonly BuildingService _buildingService;
    private readonly TechnologyService _technologyService;
    private readonly DevModeService _devModeService;

    public List<DefenseUnit> DefenseDefinitions { get; private set; } = new();
    
    // Inventory: UnitId -> Count
    public Dictionary<string, int> BuiltDefenses { get; private set; } = new();
    
    // Queue
    public List<DefenseQueueItem> ConstructionQueue { get; private set; } = new();

    public event Action? OnChange;

    public DefenseService(GameDbContext dbContext, ResourceService resourceService, BuildingService buildingService, TechnologyService technologyService, DevModeService devModeService)
    {
        _dbContext = dbContext;
        _resourceService = resourceService;
        _buildingService = buildingService;
        _technologyService = technologyService;
        _devModeService = devModeService;
        
        InitializeDefenses();
        LoadFromDatabaseAsync().Wait();
        
        // Start queue processor
        _ = ProcessQueueLoop();
    }

    private async Task LoadFromDatabaseAsync()
    {
        var dbDefenses = await _dbContext.Defenses.ToListAsync();
        foreach (var dbDefense in dbDefenses)
        {
            BuiltDefenses[dbDefense.DefenseType] = dbDefense.Quantity;
        }
    }

    private async Task SaveToDatabaseAsync()
    {
        foreach (var kvp in BuiltDefenses)
        {
            var dbDefense = await _dbContext.Defenses.FirstOrDefaultAsync(d => d.DefenseType == kvp.Key);
            if (dbDefense != null)
            {
                dbDefense.Quantity = kvp.Value;
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    private void InitializeDefenses()
    {
        DefenseDefinitions.Add(new DefenseUnit
        {
            Id = "RL", Name = "Rocket Launcher", Description = "A simple but cost-effective defense.",
            Image = "assets/defense/rocketLauncher.jpg",
            MetalCost = 2000, CrystalCost = 0, DeuteriumCost = 0,
            Structure = 2000, Shield = 20, Attack = 80,
            BaseDuration = TimeSpan.FromSeconds(20),
            Requirements = new() { { "Shipyard", 1 } }
        });

        DefenseDefinitions.Add(new DefenseUnit
        {
            Id = "LL", Name = "Light Laser", Description = "Uses concentrated light to damage enemies.",
            Image = "assets/defense/laserCannon.jpg", // Using existing image name
            MetalCost = 1500, CrystalCost = 500, DeuteriumCost = 0,
            Structure = 2000, Shield = 25, Attack = 100,
            BaseDuration = TimeSpan.FromSeconds(30),
            Requirements = new() { { "Shipyard", 2 }, { "Laser Technology", 3 } }
        });

        DefenseDefinitions.Add(new DefenseUnit
        {
            Id = "HL", Name = "Heavy Laser", Description = "Stronger version of the light laser.",
            Image = "assets/defense/heavyLaser.jpg",
            MetalCost = 6000, CrystalCost = 2000, DeuteriumCost = 0,
            Structure = 8000, Shield = 100, Attack = 250,
            BaseDuration = TimeSpan.FromSeconds(60),
            Requirements = new() { { "Shipyard", 4 }, { "Laser Technology", 6 } }
        });

        DefenseDefinitions.Add(new DefenseUnit
        {
            Id = "GC", Name = "Gauss Cannon", Description = "Fires high-velocity projectiles.",
            Image = "assets/defense/gaussCannon.jpg",
            MetalCost = 20000, CrystalCost = 15000, DeuteriumCost = 2000,
            Structure = 35000, Shield = 200, Attack = 1100,
            BaseDuration = TimeSpan.FromMinutes(2.5),
            Requirements = new() { { "Shipyard", 6 }, { "Energy Technology", 6 }, { "Weapons Technology", 3 } }
        });

        DefenseDefinitions.Add(new DefenseUnit
        {
            Id = "IC", Name = "Ion Cannon", Description = "Fires ion beams to disrupt shields.",
            Image = "assets/defense/ionCannon.jpg",
            MetalCost = 2000, CrystalCost = 6000, DeuteriumCost = 0,
            Structure = 8000, Shield = 500, Attack = 150,
            BaseDuration = TimeSpan.FromMinutes(1.5),
            Requirements = new() { { "Shipyard", 4 }, { "Ion Technology", 4 } }
        });

        DefenseDefinitions.Add(new DefenseUnit
        {
            Id = "PT", Name = "Plasma Turret", Description = "The most powerful defensive structure.",
            Image = "assets/defense/plasmaTurret.jpg",
            MetalCost = 50000, CrystalCost = 50000, DeuteriumCost = 30000,
            Structure = 100000, Shield = 300, Attack = 3000,
            BaseDuration = TimeSpan.FromMinutes(10),
            Requirements = new() { { "Shipyard", 8 }, { "Plasma Technology", 7 } }
        });

        DefenseDefinitions.Add(new DefenseUnit
        {
            Id = "SSD", Name = "Small Shield Dome", Description = "Protects the planet with a shield.",
            Image = "assets/defense/smallShield.jpg",
            MetalCost = 10000, CrystalCost = 10000, DeuteriumCost = 0,
            Structure = 20000, Shield = 2000, Attack = 0,
            BaseDuration = TimeSpan.FromMinutes(5),
            Requirements = new() { { "Shipyard", 1 }, { "Shielding Technology", 2 } }
        });

        DefenseDefinitions.Add(new DefenseUnit
        {
            Id = "LSD", Name = "Large Shield Dome", Description = "A massive planetary shield.",
            Image = "assets/defense/largeShield.jpg",
            MetalCost = 50000, CrystalCost = 50000, DeuteriumCost = 0,
            Structure = 100000, Shield = 10000, Attack = 0,
            BaseDuration = TimeSpan.FromMinutes(20),
            Requirements = new() { { "Shipyard", 6 }, { "Shielding Technology", 6 } }
        });
        
        DefenseDefinitions.Add(new DefenseUnit
        {
            Id = "ABM", Name = "Anti-Ballistic Missile", Description = "Intercepts interplanetary missiles.",
            Image = "assets/defense/abm.jpg",
            MetalCost = 8000, CrystalCost = 0, DeuteriumCost = 2000,
            Structure = 8000, Shield = 1, Attack = 1,
            BaseDuration = TimeSpan.FromMinutes(2),
            Requirements = new() { { "Shipyard", 1 }, { "Missile Silo", 2 } }
        });
        
        DefenseDefinitions.Add(new DefenseUnit
        {
            Id = "IPM", Name = "Interplanetary Missile", Description = "Destroys enemy defenses remotely.",
            Image = "assets/defense/ipm.jpg",
            MetalCost = 12500, CrystalCost = 2500, DeuteriumCost = 10000,
            Structure = 15000, Shield = 1, Attack = 12000,
            BaseDuration = TimeSpan.FromMinutes(5),
            Requirements = new() { { "Shipyard", 1 }, { "Missile Silo", 4 }, { "Impulse Drive", 1 } }
        });
    }

    public int GetDefenseCount(string id)
    {
        return BuiltDefenses.ContainsKey(id) ? BuiltDefenses[id] : 0;
    }

    public void AddToQueue(DefenseUnit unit, int quantity)
    {
        if (quantity <= 0) return;

        // Check Shield Domes (Max 1 per planet)
        if ((unit.Id == "SSD" || unit.Id == "LSD"))
        {
             int existing = GetDefenseCount(unit.Id);
             int queued = ConstructionQueue.Where(q => q.Unit.Id == unit.Id).Sum(q => q.Quantity);
             if (existing + queued + quantity > 1) 
             {
                 // Cap quantity to 1 max total
                 quantity = 1 - (existing + queued);
                 if (quantity <= 0) return;
             }
        }

        long totalMetal = unit.MetalCost * quantity;
        long totalCrystal = unit.CrystalCost * quantity;
        long totalDeuterium = unit.DeuteriumCost * quantity;

        if (_resourceService.HasResources(totalMetal, totalCrystal, totalDeuterium))
        {
            _resourceService.ConsumeResources(totalMetal, totalCrystal, totalDeuterium);
            
            // Duration logic (same as Shipyard)
            var shipyardLevel = _buildingService.GetBuildingLevel("Shipyard");
            var naniteLevel = _buildingService.GetBuildingLevel("Nanite Factory");
            
            double divisor = (1 + shipyardLevel) * Math.Pow(2, naniteLevel);
            if (divisor < 1) divisor = 1;

            double durationSeconds = unit.BaseDuration.TotalSeconds / divisor / 100.0; // x100 speed
            if (durationSeconds < 1) durationSeconds = 1;

            var finalDuration = TimeSpan.FromSeconds(durationSeconds);
            finalDuration = _devModeService.GetDuration(finalDuration, 5);

            ConstructionQueue.Add(new DefenseQueueItem
            {
                Unit = unit,
                Quantity = quantity,
                DurationPerUnit = finalDuration,
                TimeRemaining = finalDuration
            });
            
            NotifyStateChanged();
        }
    }
    
    private async Task ProcessQueueLoop()
    {
        while (true)
        {
            if (ConstructionQueue.Any())
            {
                var currentItem = ConstructionQueue.First();
                
                currentItem.TimeRemaining = currentItem.TimeRemaining.Subtract(TimeSpan.FromSeconds(1));

                if (currentItem.TimeRemaining <= TimeSpan.Zero)
                {
                    // Unit complete
                    if (!BuiltDefenses.ContainsKey(currentItem.Unit.Id))
                        BuiltDefenses[currentItem.Unit.Id] = 0;
                    
                    BuiltDefenses[currentItem.Unit.Id]++;
                    
                    currentItem.Quantity--;
                    
                    if (currentItem.Quantity > 0)
                    {
                        currentItem.TimeRemaining = currentItem.DurationPerUnit;
                    }
                    else
                    {
                        ConstructionQueue.RemoveAt(0);
                    }
                    
                    await SaveToDatabaseAsync();
                    NotifyStateChanged();
                }
                NotifyStateChanged();
            }
            
            await Task.Delay(1000);
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
