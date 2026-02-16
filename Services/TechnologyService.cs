using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;
using Microsoft.Extensions.Logging;

namespace myapp.Services;

public enum TechType
{
    Espionage = 1,
    Computer = 2,
    Weapons = 3,
    Shielding = 4,
    Armour = 5,
    Energy = 6,
    Hyperspace = 7,
    CombustionDrive = 8,
    ImpulseDrive = 9,
    HyperspaceDrive = 10,
    Laser = 11,
    Ion = 12,
    Plasma = 13,
    IntergalacticNetwork = 14,
    Astrophysics = 15,
    Graviton = 16
}

public class Requirement
{
    public string Name { get; set; } = ""; // Can be Building Title or Tech Title
    public int Level { get; set; }
    public bool IsBuilding { get; set; }
}

public class Technology
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TechType Type { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Image { get; set; } = "";
    public long BaseMetalCost { get; set; }
    public long BaseCrystalCost { get; set; }
    public long BaseDeuteriumCost { get; set; }
    public long BaseEnergyCost { get; set; }
    public TimeSpan BaseDuration { get; set; }
    public int Level { get; set; } = 0;
    public double Scaling { get; set; } = 2.0;
    public List<Requirement> Requirements { get; set; } = new();

    // Runtime state
    public TimeSpan ConstructionDuration { get; set; }
    public bool IsResearching { get; set; }
    public TimeSpan TimeRemaining { get; set; }

    // Calculated properties
    public long MetalCost => (long)(BaseMetalCost * Math.Pow(Scaling, Level));
    public long CrystalCost => (long)(BaseCrystalCost * Math.Pow(Scaling, Level));
    public long DeuteriumCost => (long)(BaseDeuteriumCost * Math.Pow(Scaling, Level));
    public long EnergyCost => (long)(BaseEnergyCost * Math.Pow(Scaling, Level));
    
    // Duration logic placeholder
    public TimeSpan Duration => BaseDuration; 
}

public class TechnologyService
{
    private readonly GameDbContext _dbContext;
    private readonly ResourceService _resourceService;
    private readonly BuildingService _buildingService;
    private readonly DevModeService _devModeService;
    private readonly ILogger<TechnologyService> _logger;

    public List<Technology> Technologies { get; private set; } = new();
    public Technology? CurrentResearch { get; private set; }

    public event Action? OnChange;
    private bool _isProcessingResearch = false;

    public TechnologyService(GameDbContext dbContext, ResourceService resourceService, BuildingService buildingService, DevModeService devModeService, ILogger<TechnologyService> logger)
    {
        _dbContext = dbContext;
        _resourceService = resourceService;
        _buildingService = buildingService;
        _devModeService = devModeService;
        _logger = logger;
        InitializeTechnologies();
        LoadFromDatabaseAsync().Wait();
    }

    private async Task LoadFromDatabaseAsync()
    {
        var dbTechs = await _dbContext.Technologies.ToListAsync();
        foreach (var dbTech in dbTechs)
        {
            var tech = Technologies.FirstOrDefault(t => t.Title == dbTech.TechnologyType);
            if (tech != null)
            {
                tech.Level = dbTech.Level;
            }
        }
    }

    private async Task SaveToDatabaseAsync()
    {
        foreach (var tech in Technologies)
        {
            var dbTech = await _dbContext.Technologies.FirstOrDefaultAsync(t => t.TechnologyType == tech.Title);
            if (dbTech != null)
            {
                dbTech.Level = tech.Level;
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    private void InitializeTechnologies()
    {
        // 1. Espionage Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Espionage,
            Title = "Espionage Technology",
            Description = "Information is power. Espionage technology helps you spy on other players.",
            BaseMetalCost = 200,
            BaseCrystalCost = 1000,
            BaseDeuteriumCost = 200,
            BaseDuration = TimeSpan.FromMinutes(2),
            Image = "assets/technologies/tech_espionage.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 3, IsBuilding = true }
            }
        });

        // 2. Computer Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Computer,
            Title = "Computer Technology",
            Description = "Increases fleet slots and allows control of more fleets.",
            BaseMetalCost = 0,
            BaseCrystalCost = 400,
            BaseDeuteriumCost = 600,
            BaseDuration = TimeSpan.FromMinutes(5),
            Image = "assets/technologies/tech_computer.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 1, IsBuilding = true }
            }
        });

        // 3. Weapons Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Weapons,
            Title = "Weapons Technology",
            Description = "Improves the weapon systems of ships and defense.",
            BaseMetalCost = 800,
            BaseCrystalCost = 200,
            BaseDeuteriumCost = 0,
            BaseDuration = TimeSpan.FromMinutes(10),
            Image = "assets/technologies/tech_weapons.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 4, IsBuilding = true }
            }
        });

        // 4. Shielding Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Shielding,
            Title = "Shielding Technology",
            Description = "Improves the shields of ships and defense.",
            BaseMetalCost = 200,
            BaseCrystalCost = 600,
            BaseDeuteriumCost = 0,
            BaseDuration = TimeSpan.FromMinutes(10),
            Image = "assets/technologies/tech_shielding.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 6, IsBuilding = true },
                new Requirement { Name = "Energy Technology", Level = 3, IsBuilding = false }
            }
        });

        // 5. Armour Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Armour,
            Title = "Armour Technology",
            Description = "Improves the structural integrity of ships and defense.",
            BaseMetalCost = 1000,
            BaseCrystalCost = 0,
            BaseDeuteriumCost = 0,
            BaseDuration = TimeSpan.FromMinutes(10),
            Image = "assets/technologies/tech_armour.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 2, IsBuilding = true }
            }
        });

        // 6. Energy Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Energy,
            Title = "Energy Technology",
            Description = "Fundamental for advanced power plants and weapons.",
            BaseMetalCost = 0,
            BaseCrystalCost = 800,
            BaseDeuteriumCost = 400,
            BaseDuration = TimeSpan.FromMinutes(8),
            Image = "assets/technologies/tech_energy.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 1, IsBuilding = true }
            }
        });

        // 7. Hyperspace Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Hyperspace,
            Title = "Hyperspace Technology",
            Description = "Allows travel through hyperspace dimensions.",
            BaseMetalCost = 0,
            BaseCrystalCost = 4000,
            BaseDeuteriumCost = 2000,
            BaseDuration = TimeSpan.FromMinutes(20),
            Image = "assets/technologies/tech_hyperspace.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 7, IsBuilding = true },
                new Requirement { Name = "Energy Technology", Level = 5, IsBuilding = false },
                new Requirement { Name = "Shielding Technology", Level = 5, IsBuilding = false }
            }
        });

        // 8. Combustion Drive
        Technologies.Add(new Technology
        {
            Type = TechType.CombustionDrive,
            Title = "Combustion Drive",
            Description = "Basic propulsion for ships like Light Fighters and Transporters.",
            BaseMetalCost = 400,
            BaseCrystalCost = 0,
            BaseDeuteriumCost = 600,
            BaseDuration = TimeSpan.FromMinutes(4),
            Image = "assets/technologies/tech_combustion.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 1, IsBuilding = true },
                new Requirement { Name = "Energy Technology", Level = 1, IsBuilding = false }
            }
        });

        // 9. Impulse Drive
        Technologies.Add(new Technology
        {
            Type = TechType.ImpulseDrive,
            Title = "Impulse Drive",
            Description = "Faster propulsion for Cruisers and Heavy Fighters.",
            BaseMetalCost = 2000,
            BaseCrystalCost = 4000,
            BaseDeuteriumCost = 600,
            BaseDuration = TimeSpan.FromMinutes(12),
            Image = "assets/technologies/tech_impulse.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 2, IsBuilding = true },
                new Requirement { Name = "Energy Technology", Level = 1, IsBuilding = false }
            }
        });

        // 10. Hyperspace Drive
        Technologies.Add(new Technology
        {
            Type = TechType.HyperspaceDrive,
            Title = "Hyperspace Drive",
            Description = "Advanced propulsion for Battleships and Destroyers.",
            BaseMetalCost = 10000,
            BaseCrystalCost = 20000,
            BaseDeuteriumCost = 6000,
            BaseDuration = TimeSpan.FromMinutes(45),
            Image = "assets/technologies/tech_hyperspacedrive.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 7, IsBuilding = true },
                new Requirement { Name = "Hyperspace Technology", Level = 3, IsBuilding = false }
            }
        });

        // 11. Laser Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Laser,
            Title = "Laser Technology",
            Description = "Required for advanced weapons.",
            BaseMetalCost = 200,
            BaseCrystalCost = 100,
            BaseDeuteriumCost = 0,
            BaseDuration = TimeSpan.FromMinutes(3),
            Image = "assets/technologies/tech_laser.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 1, IsBuilding = true },
                new Requirement { Name = "Energy Technology", Level = 2, IsBuilding = false }
            }
        });

        // 12. Ion Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Ion,
            Title = "Ion Technology",
            Description = "Required for Ion Cannons and Cruisers.",
            BaseMetalCost = 1000,
            BaseCrystalCost = 300,
            BaseDeuteriumCost = 100,
            BaseDuration = TimeSpan.FromMinutes(15),
            Image = "assets/technologies/tech_ion.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 4, IsBuilding = true },
                new Requirement { Name = "Laser Technology", Level = 5, IsBuilding = false },
                new Requirement { Name = "Energy Technology", Level = 4, IsBuilding = false }
            }
        });

        // 13. Plasma Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Plasma,
            Title = "Plasma Technology",
            Description = "Required for Plasma Turrets and Bombers.",
            BaseMetalCost = 2000,
            BaseCrystalCost = 4000,
            BaseDeuteriumCost = 1000,
            BaseDuration = TimeSpan.FromMinutes(30),
            Image = "assets/technologies/tech_plasma.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 4, IsBuilding = true },
                new Requirement { Name = "Energy Technology", Level = 8, IsBuilding = false },
                new Requirement { Name = "Laser Technology", Level = 10, IsBuilding = false },
                new Requirement { Name = "Ion Technology", Level = 5, IsBuilding = false }
            }
        });

        // 14. Intergalactic Research Network
        Technologies.Add(new Technology
        {
            Type = TechType.IntergalacticNetwork,
            Title = "Intergalactic Research Network",
            Description = "Links research labs on different planets.",
            BaseMetalCost = 240000,
            BaseCrystalCost = 400000,
            BaseDeuteriumCost = 160000,
            BaseDuration = TimeSpan.FromHours(2),
            Image = "assets/technologies/tech_network.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 10, IsBuilding = true },
                new Requirement { Name = "Computer Technology", Level = 8, IsBuilding = false },
                new Requirement { Name = "Hyperspace Technology", Level = 8, IsBuilding = false }
            }
        });

        // 15. Astrophysics
        Technologies.Add(new Technology
        {
            Type = TechType.Astrophysics,
            Title = "Astrophysics",
            Description = "Allows colonization of new planets and finding more on expeditions.",
            BaseMetalCost = 4000,
            BaseCrystalCost = 8000,
            BaseDeuteriumCost = 4000,
            BaseDuration = TimeSpan.FromMinutes(40),
            Scaling = 1.75, // Special scaling
            Image = "assets/technologies/tech_astrophysics.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 3, IsBuilding = true },
                new Requirement { Name = "Espionage Technology", Level = 4, IsBuilding = false },
                new Requirement { Name = "Impulse Drive", Level = 3, IsBuilding = false }
            }
        });

        // 16. Graviton Technology
        Technologies.Add(new Technology
        {
            Type = TechType.Graviton,
            Title = "Graviton Technology",
            Description = "Required for Death Star. Researched instantly if energy is sufficient.",
            BaseMetalCost = 0,
            BaseCrystalCost = 0,
            BaseDeuteriumCost = 0,
            BaseEnergyCost = 300000,
            BaseDuration = TimeSpan.Zero, // Instant
            Scaling = 3.0, // Special scaling
            Image = "assets/technologies/tech_graviton.jpg",
            Requirements = new() {
                new Requirement { Name = "Research Lab", Level = 12, IsBuilding = true }
            }
        });
    }
    
    public bool RequirementsMet(Technology tech)
    {
        foreach(var req in tech.Requirements)
        {
            if (req.IsBuilding)
            {
                if (_buildingService.GetBuildingLevel(req.Name) < req.Level) return false;
            }
            else
            {
                // Recursive tech check might be needed if tech tree is complex, 
                // but usually checking the flat list of tech levels is enough for OGame.
                var requiredTech = Technologies.FirstOrDefault(t => t.Title == req.Name);
                if (requiredTech == null || requiredTech.Level < req.Level) return false;
            }
        }
        return true;
    }

    public void StartResearch(Technology tech)
    {
        // Check Requirements
        if (!RequirementsMet(tech)) return;

        // Only one research at a time per user (simplified)
        if (CurrentResearch != null) return;

        // Ensure resources update before check
        _buildingService.UpdateProduction();

        // Graviton Check (Energy instead of resources)
        if (tech.BaseEnergyCost > 0)
        {
             if (_resourceService.Energy >= tech.EnergyCost)
             {
                 // Instant complete
                 tech.Level++;
                 SaveToDatabaseAsync().Wait();
                 NotifyStateChanged();
             }
             return;
        }

        if (_resourceService.HasResources(tech.MetalCost, tech.CrystalCost, tech.DeuteriumCost))
        {
            _resourceService.ConsumeResources(tech.MetalCost, tech.CrystalCost, tech.DeuteriumCost);

            CurrentResearch = tech;
            tech.IsResearching = true;
            
            var duration = _devModeService.GetDuration(tech.Duration, 1);
            tech.ConstructionDuration = duration;
            tech.TimeRemaining = duration;
            
            _logger.LogInformation("StartResearch: {Tech}, DevMode={DevMode}, Duration={Duration}s", 
                tech.Title, _devModeService.IsEnabled, duration.TotalSeconds);
            
            NotifyStateChanged();
            
            if (!_isProcessingResearch)
            {
                _ = ProcessResearchQueue();
            }
        }
    }

    public void CancelResearch()
    {
        if (CurrentResearch != null)
        {
            // Refund resources based on CancelRefundPercentage setting
            _resourceService.RefundResources(
                CurrentResearch.MetalCost, 
                CurrentResearch.CrystalCost, 
                CurrentResearch.DeuteriumCost);
            
            CurrentResearch.IsResearching = false;
            CurrentResearch.TimeRemaining = TimeSpan.Zero;
            CurrentResearch = null;
            NotifyStateChanged();
        }
    }

    private async Task ProcessResearchQueue()
    {
        _isProcessingResearch = true;
        _logger.LogInformation("ProcessResearchQueue started");

        try
        {
            while (CurrentResearch != null)
            {
                var tech = CurrentResearch;
                tech.IsResearching = true;
                NotifyStateChanged();

                _logger.LogInformation("Processing research: {Tech}, TimeRemaining={Time}s", 
                    tech.Title, tech.TimeRemaining.TotalSeconds);

                while (tech.TimeRemaining > TimeSpan.Zero)
                {
                    // Check if this research was cancelled
                    if (CurrentResearch != tech) break;

                    await Task.Delay(1000);

                    // Re-check after delay
                    if (CurrentResearch != tech) break;

                    tech.TimeRemaining = tech.TimeRemaining.Subtract(TimeSpan.FromSeconds(1));
                    NotifyStateChanged();
                }

                // Check if cancelled (CurrentResearch changed or was nulled)
                if (CurrentResearch != tech) continue;

                // Complete
                _logger.LogInformation("Research completed: {Tech} -> Level {Level}", tech.Title, tech.Level + 1);
                tech.IsResearching = false;
                tech.Level++;
                await SaveToDatabaseAsync();
                CurrentResearch = null;

                NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessResearchQueue: {Message}", ex.Message);
        }
        finally
        {
            _isProcessingResearch = false;
            _logger.LogInformation("ProcessResearchQueue ended");
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
    
    public int GetTechLevel(TechType type)
    {
        var tech = Technologies.FirstOrDefault(t => t.Type == type);
        return tech?.Level ?? 0;
    }
}
