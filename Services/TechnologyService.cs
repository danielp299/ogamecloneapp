using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace myapp.Services;

public class Technology
{
    public Guid Id { get; set; } = Guid.NewGuid();
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
    public List<string> Dependencies { get; set; } = new();

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
    private readonly BuildingService _buildingService; // For checking Lab levels

    public List<Technology> Technologies { get; private set; } = new();
    public Technology? CurrentResearch { get; private set; }

    public event Action OnChange;
    private bool _isProcessingResearch = false;

    public TechnologyService(ResourceService resourceService, BuildingService buildingService)
    {
        _resourceService = resourceService;
        _buildingService = buildingService;
        InitializeTechnologies();
    }

    private void InitializeTechnologies()
    {
        // 1. Espionage Technology
        Technologies.Add(new Technology
        {
            Title = "Espionage Technology",
            Description = "Information is power. Espionage technology helps you spy on other players.",
            BaseMetalCost = 200,
            BaseCrystalCost = 1000,
            BaseDeuteriumCost = 200,
            BaseDuration = TimeSpan.FromMinutes(2),
            Image = "tech_espionage.jpg"
        });

        // 2. Computer Technology
        Technologies.Add(new Technology
        {
            Title = "Computer Technology",
            Description = "Increases fleet slots and allows control of more fleets.",
            BaseMetalCost = 0,
            BaseCrystalCost = 400,
            BaseDeuteriumCost = 600,
            BaseDuration = TimeSpan.FromMinutes(5),
            Image = "tech_computer.jpg"
        });

        // 3. Weapons Technology
        Technologies.Add(new Technology
        {
            Title = "Weapons Technology",
            Description = "Improves the weapon systems of ships and defense.",
            BaseMetalCost = 800,
            BaseCrystalCost = 200,
            BaseDeuteriumCost = 0,
            BaseDuration = TimeSpan.FromMinutes(10),
            Image = "tech_weapons.jpg"
        });

        // 4. Shielding Technology
        Technologies.Add(new Technology
        {
            Title = "Shielding Technology",
            Description = "Improves the shields of ships and defense.",
            BaseMetalCost = 200,
            BaseCrystalCost = 600,
            BaseDeuteriumCost = 0,
            BaseDuration = TimeSpan.FromMinutes(10),
            Image = "tech_shielding.jpg"
        });

        // 5. Armour Technology
        Technologies.Add(new Technology
        {
            Title = "Armour Technology",
            Description = "Improves the structural integrity of ships and defense.",
            BaseMetalCost = 1000,
            BaseCrystalCost = 0,
            BaseDeuteriumCost = 0,
            BaseDuration = TimeSpan.FromMinutes(10),
            Image = "tech_armour.jpg"
        });

        // 6. Energy Technology
        Technologies.Add(new Technology
        {
            Title = "Energy Technology",
            Description = "Fundamental for advanced power plants and weapons.",
            BaseMetalCost = 0,
            BaseCrystalCost = 800,
            BaseDeuteriumCost = 400,
            BaseDuration = TimeSpan.FromMinutes(8),
            Image = "tech_energy.jpg"
        });

        // 7. Hyperspace Technology
        Technologies.Add(new Technology
        {
            Title = "Hyperspace Technology",
            Description = "Allows travel through hyperspace dimensions.",
            BaseMetalCost = 0,
            BaseCrystalCost = 4000,
            BaseDeuteriumCost = 2000,
            BaseDuration = TimeSpan.FromMinutes(20),
            Image = "tech_hyperspace.jpg"
        });

        // 8. Combustion Drive
        Technologies.Add(new Technology
        {
            Title = "Combustion Drive",
            Description = "Basic propulsion for ships like Light Fighters and Transporters.",
            BaseMetalCost = 400,
            BaseCrystalCost = 0,
            BaseDeuteriumCost = 600,
            BaseDuration = TimeSpan.FromMinutes(4),
            Image = "tech_combustion.jpg"
        });

        // 9. Impulse Drive
        Technologies.Add(new Technology
        {
            Title = "Impulse Drive",
            Description = "Faster propulsion for Cruisers and Heavy Fighters.",
            BaseMetalCost = 2000,
            BaseCrystalCost = 4000,
            BaseDeuteriumCost = 600,
            BaseDuration = TimeSpan.FromMinutes(12),
            Image = "tech_impulse.jpg"
        });

        // 10. Hyperspace Drive
        Technologies.Add(new Technology
        {
            Title = "Hyperspace Drive",
            Description = "Advanced propulsion for Battleships and Destroyers.",
            BaseMetalCost = 10000,
            BaseCrystalCost = 20000,
            BaseDeuteriumCost = 6000,
            BaseDuration = TimeSpan.FromMinutes(45),
            Image = "tech_hyperspacedrive.jpg"
        });

        // 11. Laser Technology
        Technologies.Add(new Technology
        {
            Title = "Laser Technology",
            Description = "Required for advanced weapons.",
            BaseMetalCost = 200,
            BaseCrystalCost = 100,
            BaseDeuteriumCost = 0,
            BaseDuration = TimeSpan.FromMinutes(3),
            Image = "tech_laser.jpg"
        });

        // 12. Ion Technology
        Technologies.Add(new Technology
        {
            Title = "Ion Technology",
            Description = "Required for Ion Cannons and Cruisers.",
            BaseMetalCost = 1000,
            BaseCrystalCost = 300,
            BaseDeuteriumCost = 100,
            BaseDuration = TimeSpan.FromMinutes(15),
            Image = "tech_ion.jpg"
        });

        // 13. Plasma Technology
        Technologies.Add(new Technology
        {
            Title = "Plasma Technology",
            Description = "Required for Plasma Turrets and Bombers.",
            BaseMetalCost = 2000,
            BaseCrystalCost = 4000,
            BaseDeuteriumCost = 1000,
            BaseDuration = TimeSpan.FromMinutes(30),
            Image = "tech_plasma.jpg"
        });

        // 14. Intergalactic Research Network
        Technologies.Add(new Technology
        {
            Title = "Intergalactic Research Network",
            Description = "Links research labs on different planets.",
            BaseMetalCost = 240000,
            BaseCrystalCost = 400000,
            BaseDeuteriumCost = 160000,
            BaseDuration = TimeSpan.FromHours(2),
            Image = "tech_network.jpg"
        });

        // 15. Astrophysics
        Technologies.Add(new Technology
        {
            Title = "Astrophysics",
            Description = "Allows colonization of new planets.",
            BaseMetalCost = 4000,
            BaseCrystalCost = 8000,
            BaseDeuteriumCost = 4000,
            BaseDuration = TimeSpan.FromMinutes(40),
            Scaling = 1.75, // Special scaling
            Image = "tech_astrophysics.jpg"
        });

        // 16. Graviton Technology
        Technologies.Add(new Technology
        {
            Title = "Graviton Technology",
            Description = "Required for Death Star. Researched instantly if energy is sufficient.",
            BaseMetalCost = 0,
            BaseCrystalCost = 0,
            BaseDeuteriumCost = 0,
            BaseEnergyCost = 300000,
            BaseDuration = TimeSpan.Zero, // Instant
            Scaling = 3.0, // Special scaling
            Image = "tech_graviton.jpg"
        });
    }

    public void StartResearch(Technology tech)
    {
        // Only one research at a time per user (simplified)
        if (CurrentResearch != null) return;

        // Ensure resources update before check
        _buildingService.UpdateProduction();

        // Graviton Check (Energy instead of resources)
        if (tech.BaseEnergyCost > 0)
        {
             // Usually Graviton is instant if you have the energy.
             // We need to check current Energy (Available).
             // ResourceService.Energy is Net Energy.
             // Usually Graviton requires *Producing* that much energy, not *Spending* it permanently?
             // Actually in OGame you just need to "Have" the energy available at that moment.
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
            tech.ConstructionDuration = tech.Duration; // Simplify duration logic for now
            tech.TimeRemaining = tech.ConstructionDuration;
            
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
}
