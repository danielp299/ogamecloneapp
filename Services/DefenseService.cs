using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

// Refer to wiki/business-rules/Defense.md for business rules documentation
// Refer to wiki/business-rules/Factory.md for shipyard and defense construction rules

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
    public Guid Id { get; set; } = Guid.NewGuid();
    public DefenseUnit Unit { get; set; } = null!;
    public int Quantity { get; set; }
    public int QuantityCompleted { get; set; }
    public TimeSpan DurationPerUnit { get; set; }
    public TimeSpan TimeRemaining { get; set; }
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }
}

public class DefenseService
{
    private readonly GameDbContext _dbContext;
    private readonly ResourceService _resourceService;
    private readonly BuildingService _buildingService;
    private readonly TechnologyService _technologyService;
    private readonly DevModeService _devModeService;
    private readonly EnemyService _enemyService;
    private readonly PlayerStateService _playerStateService;
    private readonly RankingService _rankingService;

    public List<DefenseUnit> DefenseDefinitions { get; private set; } = new();
    
    // Inventory: UnitId -> Count
    public Dictionary<string, int> BuiltDefenses { get; private set; } = new();
    
    // Queue
    private List<DefenseQueueItem> _allConstructionQueue = new();
    public List<DefenseQueueItem> ConstructionQueue => _allConstructionQueue
        .Where(i => i.Galaxy == _playerStateService.ActiveGalaxy && i.System == _playerStateService.ActiveSystem && i.Position == _playerStateService.ActivePosition)
        .ToList();

    public event Action? OnChange;
    private bool _isInitialized = false;

    public DefenseService(GameDbContext dbContext, ResourceService resourceService, BuildingService buildingService, TechnologyService technologyService, DevModeService devModeService, EnemyService enemyService, PlayerStateService playerStateService, RankingService? rankingService = null)
    {
        _dbContext = dbContext;
        _resourceService = resourceService;
        _buildingService = buildingService;
        _technologyService = technologyService;
        _devModeService = devModeService;
        _enemyService = enemyService;
        _playerStateService = playerStateService;
        _rankingService = rankingService;
        
        InitializeDefenses();

        _playerStateService.OnChange += async () => 
        {
            await LoadFromDatabaseAsync();
            await LoadConstructionQueueAsync();
            NotifyStateChanged();
        };
        
        // Start queue processor
        _ = ProcessQueueLoop();
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await LoadFromDatabaseAsync();
        await LoadConstructionQueueAsync();
        _isInitialized = true;
    }

    private async Task LoadFromDatabaseAsync()
    {
        int g = _playerStateService.ActiveGalaxy;
        int s = _playerStateService.ActiveSystem;
        int p = _playerStateService.ActivePosition;

        var dbDefenses = await _dbContext.Defenses
            .Where(d => d.Galaxy == g && d.System == s && d.Position == p)
            .ToListAsync();
        
        // Reset counts
        foreach (var def in DefenseDefinitions) BuiltDefenses[def.Id] = 0;

        foreach (var dbDefense in dbDefenses)
        {
            BuiltDefenses[dbDefense.DefenseType] = dbDefense.Quantity;
        }
    }

    private async Task LoadConstructionQueueAsync()
    {
        var dbQueue = await _dbContext.DefenseQueue
            .OrderBy(q => q.Galaxy)
            .ThenBy(q => q.System)
            .ThenBy(q => q.Position)
            .ThenBy(q => q.StartTime)
            .ThenBy(q => q.Id)
            .ToListAsync();

        _allConstructionQueue = dbQueue
            .Select(q =>
            {
                var unit = DefenseDefinitions.FirstOrDefault(d => d.Id == q.DefenseType);
                if (unit == null) return null;

                int remainingQuantity = q.Quantity - q.QuantityCompleted;
                if (remainingQuantity <= 0) return null;

                var durationPerUnit = q.EndTime - q.StartTime;
                if (durationPerUnit <= TimeSpan.Zero)
                {
                    durationPerUnit = _devModeService.GetDuration(CalculateDefenseConstructionDuration(unit), 1);
                }

                return new DefenseQueueItem
                {
                    Id = q.Id,
                    Unit = unit,
                    Quantity = remainingQuantity,
                    QuantityCompleted = q.QuantityCompleted,
                    DurationPerUnit = durationPerUnit,
                    TimeRemaining = q.IsProcessing && !q.IsCompleted ? q.TimeRemaining : durationPerUnit,
                    Galaxy = q.Galaxy,
                    System = q.System,
                    Position = q.Position
                };
            })
            .Where(q => q != null)
            .Cast<DefenseQueueItem>()
            .ToList();
    }

    private async Task SaveToDatabaseAsync()
    {
        int g = _playerStateService.ActiveGalaxy;
        int s = _playerStateService.ActiveSystem;
        int p = _playerStateService.ActivePosition;

        foreach (var kvp in BuiltDefenses)
        {
            var dbDefense = await _dbContext.Defenses.FirstOrDefaultAsync(d => 
                d.DefenseType == kvp.Key && d.Galaxy == g && d.System == s && d.Position == p);
            
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

    public TimeSpan CalculateDefenseConstructionDuration(DefenseUnit unit)
    {
        int shipyardLevel = _buildingService.GetBuildingLevel("Shipyard");
        int naniteLevel = _buildingService.GetBuildingLevel("Nanite Factory");
        
        long metalCost = unit.MetalCost;
        long crystalCost = unit.CrystalCost;
        
        // Formula: Time(hours) = (Metal + Crystal) / (5000 * (1 + Shipyard) * 2^Nanite * UniverseSpeed)
        double universeSpeed = 1.0;
        double divisor = 5000.0 * (1.0 + shipyardLevel) * Math.Pow(2, naniteLevel) * universeSpeed;
        
        double hours = (metalCost + crystalCost) / divisor;
        double seconds = hours * 3600.0;
        
        return TimeSpan.FromSeconds(seconds);
    }

    public async Task AddToQueueAsync(DefenseUnit unit, int quantity)
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

        if (await _resourceService.HasResourcesAsync(totalMetal, totalCrystal, totalDeuterium))
        {
            await _resourceService.ConsumeResourcesAsync(totalMetal, totalCrystal, totalDeuterium);
            _rankingService?.AddSpendingPoints(RankingService.PlayerKey, RankingService.PlayerName, false, totalMetal, totalCrystal, totalDeuterium);

            var calculatedDuration = CalculateDefenseConstructionDuration(unit);
            var finalDuration = _devModeService.GetDuration(calculatedDuration, 1);

            int g = _playerStateService.ActiveGalaxy;
            int s = _playerStateService.ActiveSystem;
            int p = _playerStateService.ActivePosition;

            _allConstructionQueue.Add(new DefenseQueueItem
            {
                Unit = unit,
                Quantity = quantity,
                QuantityCompleted = 0,
                DurationPerUnit = finalDuration,
                TimeRemaining = finalDuration,
                Galaxy = g,
                System = s,
                Position = p
            });
            
            await PersistQueueForPlanetAsync(g, s, p);

            // Notify enemy service that player is building defenses
            _ = _enemyService.OnPlayerDefenseBuilt(unit.Name, quantity);
            
            NotifyStateChanged();
        }
    }
    

    public async Task CancelQueueItemAsync(DefenseQueueItem item)
    {
        if (!_allConstructionQueue.Contains(item)) return;

        long refundMetal = item.Unit.MetalCost * item.Quantity;
        long refundCrystal = item.Unit.CrystalCost * item.Quantity;
        long refundDeuterium = item.Unit.DeuteriumCost * item.Quantity;

        await _resourceService.RefundResourcesAsync(refundMetal, refundCrystal, refundDeuterium);
        _allConstructionQueue.Remove(item);
        await PersistQueueForPlanetAsync(item.Galaxy, item.System, item.Position);
        NotifyStateChanged();
    }
    private async Task ProcessQueueLoop()
    {
        while (true)
        {
            foreach (var queueGroup in _allConstructionQueue.GroupBy(i => $"{i.Galaxy}:{i.System}:{i.Position}").ToList())
            {
                var currentItem = queueGroup.FirstOrDefault();
                if (currentItem == null) continue;

                currentItem.TimeRemaining = currentItem.TimeRemaining.Subtract(TimeSpan.FromSeconds(1));

                if (currentItem.TimeRemaining <= TimeSpan.Zero)
                {
                    await AddBuiltDefenseToPlanetAsync(currentItem.Unit.Id, currentItem.Galaxy, currentItem.System, currentItem.Position);

                    currentItem.Quantity--;
                    currentItem.QuantityCompleted++;

                    if (currentItem.Quantity > 0)
                    {
                        currentItem.TimeRemaining = currentItem.DurationPerUnit;
                    }
                    else
                    {
                        _allConstructionQueue.Remove(currentItem);
                    }

                    await PersistQueueForPlanetAsync(currentItem.Galaxy, currentItem.System, currentItem.Position);

                    if (currentItem.Galaxy == _playerStateService.ActiveGalaxy && currentItem.System == _playerStateService.ActiveSystem && currentItem.Position == _playerStateService.ActivePosition)
                    {
                        await LoadFromDatabaseAsync();
                    }

                    NotifyStateChanged();
                }
            }
            
            NotifyStateChanged();
            await Task.Delay(1000);
        }
    }


    private async Task AddBuiltDefenseToPlanetAsync(string defenseId, int g, int s, int p)
    {
        var dbDefense = await _dbContext.Defenses.FirstOrDefaultAsync(d =>
            d.DefenseType == defenseId && d.Galaxy == g && d.System == s && d.Position == p);

        if (dbDefense != null)
        {
            dbDefense.Quantity++;
        }
        else
        {
            _dbContext.Defenses.Add(new DefenseEntity
            {
                DefenseType = defenseId,
                Quantity = 1,
                Galaxy = g,
                System = s,
                Position = p
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task PersistQueueForPlanetAsync(int g, int s, int p)
    {
        var planetQueue = _allConstructionQueue
            .Where(i => i.Galaxy == g && i.System == s && i.Position == p)
            .OrderBy(i => i.Id)
            .ToList();

        var existing = await _dbContext.DefenseQueue
            .Where(i => i.Galaxy == g && i.System == s && i.Position == p)
            .ToListAsync();

        if (existing.Any())
        {
            _dbContext.DefenseQueue.RemoveRange(existing);
            await _dbContext.SaveChangesAsync();
        }

        if (!planetQueue.Any()) return;

        DateTime nextJobStart = DateTime.UtcNow;
        var entities = new List<DefenseQueueEntity>();

        for (int i = 0; i < planetQueue.Count; i++)
        {
            var item = planetQueue[i];
            item.TimeRemaining = i == 0 ? GetRemainingDuration(item.TimeRemaining, item.DurationPerUnit) : item.DurationPerUnit;

            DateTime currentUnitStart = nextJobStart;
            DateTime currentUnitEnd = currentUnitStart.Add(item.TimeRemaining);
            TimeSpan totalRemainingDuration = item.TimeRemaining + TimeSpan.FromTicks(item.DurationPerUnit.Ticks * Math.Max(0, item.Quantity - 1));
            nextJobStart = currentUnitStart.Add(totalRemainingDuration);

            entities.Add(new DefenseQueueEntity
            {
                Id = item.Id,
                DefenseType = item.Unit.Id,
                Quantity = item.Quantity + item.QuantityCompleted,
                QuantityCompleted = item.QuantityCompleted,
                Galaxy = g,
                System = s,
                Position = p,
                StartTime = currentUnitStart,
                EndTime = currentUnitEnd,
                IsProcessing = i == 0
            });
        }

        await _dbContext.DefenseQueue.AddRangeAsync(entities);
        await _dbContext.SaveChangesAsync();
    }

    private static TimeSpan GetRemainingDuration(TimeSpan remaining, TimeSpan fallback)
    {
        if (remaining <= TimeSpan.Zero) return fallback;
        if (remaining > fallback) return fallback;
        return remaining;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void ResetState()
    {
        DefenseDefinitions.Clear();
        BuiltDefenses.Clear();
        _allConstructionQueue.Clear();
        InitializeDefenses();
    }
}

