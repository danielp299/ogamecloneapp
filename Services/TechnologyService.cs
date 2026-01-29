using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    public string Name { get; set; } // Can be Building Title or Tech Title
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
    private readonly ResourceService _resourceService;
    private readonly BuildingService _buildingService;
    private readonly DevModeService _devModeService;

    public List<Technology> Technologies { get; private set; } = new();
    public Technology? CurrentResearch { get; private set; }

    public event Action OnChange;
    private bool _isProcessingResearch = false;

    public TechnologyService(ResourceService resourceService, BuildingService buildingService, DevModeService devModeService)
    {
        _resourceService = resourceService;
        _buildingService = buildingService;
        _devModeService = devModeService;
        InitializeTechnologies();
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
            Image = "tech_espionage.jpg",
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
            Image = "tech_computer.jpg",
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
            Image = "tech_weapons.jpg",
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
            Image = "tech_shielding.jpg",
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
            Image = "tech_armour.jpg",
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
            Image = "tech_energy.jpg",
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
            Image = "tech_hyperspace.jpg",
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
            Image = "tech_combustion.jpg",
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
            Image = "tech_impulse.jpg",
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
            Image = "tech_hyperspacedrive.jpg",
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
            Image = "tech_laser.jpg",
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
            Image = "tech_ion.jpg",
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
            Image = "tech_plasma.jpg",
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
            Image = "tech_network.jpg",
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
            Image = "tech_astrophysics.jpg",
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
            Image = "tech_graviton.jpg",
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
                 NotifyStateChanged();
             }
             return;
        }

        if (_resourceService.HasResources(tech.MetalCost, tech.CrystalCost, tech.DeuteriumCost))
        {
            _resourceService.ConsumeResources(tech.MetalCost, tech.CrystalCost, tech.DeuteriumCost);

            CurrentResearch = tech;
            tech.IsResearching = true;
            
            var duration = _devModeService.GetDuration(tech.Duration, 10);
            tech.ConstructionDuration = duration;
            tech.TimeRemaining = duration;
            
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
            CurrentResearch.IsResearching = false;
            CurrentResearch.TimeRemaining = TimeSpan.Zero;
            // Refund logic could go here
            CurrentResearch = null;
            NotifyStateChanged();
        }
    }

    private async Task ProcessResearchQueue()
    {
        _isProcessingResearch = true;

        while (CurrentResearch != null)
        {
            NotifyStateChanged();

            while (CurrentResearch.TimeRemaining > TimeSpan.Zero)
            {
                // Check cancellation
                if (CurrentResearch == null || !CurrentResearch.IsResearching) break;

                await Task.Delay(1000);
                CurrentResearch.TimeRemaining = CurrentResearch.TimeRemaining.Subtract(TimeSpan.FromSeconds(1));
                NotifyStateChanged();
            }

            if (CurrentResearch == null || !CurrentResearch.IsResearching) continue;

            // Complete
            CurrentResearch.IsResearching = false;
            CurrentResearch.Level++;
            CurrentResearch = null; // Queue clear
            
            NotifyStateChanged();
        }

        _isProcessingResearch = false;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
    
    public int GetTechLevel(TechType type)
    {
        var tech = Technologies.FirstOrDefault(t => t.Type == type);
        return tech?.Level ?? 0;
    }
}
