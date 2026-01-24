using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace myapp.Services;

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
    // Duration logic can be improved later
    public TimeSpan Duration => BaseDuration; 
}

public class BuildingService
{
    private readonly ResourceService _resourceService;
    public List<Building> Buildings { get; private set; } = new();
    public List<Building> ConstructionQueue { get; private set; } = new();
    
    public event Action OnChange;

    private bool _isProcessingQueue = false;

    public BuildingService(ResourceService resourceService)
    {
        _resourceService = resourceService;
        InitializeBuildings();
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
            Image = "building1.jpg",
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
            Image = "building2.jpg",
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
            Image = "building3.jpg",
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
            Image = "building4.jpg",
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
            Image = "building5.jpg",
            EnergyConsumption = 30
        });

        Buildings.Add(new Building
        {
            Title = "Shipyard",
            Description = "Builds ships.",
            BaseMetalCost = 400,
            BaseCrystalCost = 200,
            BaseDeuteriumCost = 100,    
            ConstructionDuration = TimeSpan.Parse("00:01:30"),
            Image = "building6.jpg",
            EnergyConsumption = 25
        });
        Buildings.Add(new Building
        {
            Title = "Metal Storage",
            Description = "Stores Metal.",
            BaseMetalCost = 200,
            BaseCrystalCost = 0,
            BaseDeuteriumCost = 0,    
            ConstructionDuration = TimeSpan.Parse("00:00:45"),
            Image = "building7.jpg",
            EnergyConsumption = 0
        });
        Buildings.Add(new Building
        {
            Title = "Crystal Storage",
            Description = "Stores Crystal.",
            BaseMetalCost = 200,
            BaseCrystalCost = 100,
            BaseDeuteriumCost = 0,    
            ConstructionDuration = TimeSpan.Parse("00:00:50"),
            Image = "building8.jpg",
            EnergyConsumption = 0
        });
        Buildings.Add(new Building
        {
            Title = "Deuterium Storage",
            Description = "Stores Deuterium.",
            BaseMetalCost = 200,
            BaseCrystalCost = 200,
            BaseDeuteriumCost = 0,    
            ConstructionDuration = TimeSpan.Parse("00:00:55"),
            Image = "building9.jpg",
            EnergyConsumption = 0
        });
        Buildings.Add(new Building
        {
            Title = "Research Lab",
            Description = "Allows to do Research.",
            BaseMetalCost = 400,
            BaseCrystalCost = 200,
            BaseDeuteriumCost = 100,    
            ConstructionDuration = TimeSpan.Parse("00:01:15"),
            Image = "building10.jpg",
            EnergyConsumption = 20
        });

        Buildings.Add(new Building
        {
            Title = "Terraformer",
            Description = "Allows to build more buildings.",
            BaseMetalCost = 50000,
            BaseCrystalCost = 100000,
            BaseDeuteriumCost = 1000,    
            ConstructionDuration = TimeSpan.Parse("01:00:00"),
            Image = "building11.jpg",
            EnergyConsumption = 50
        });

        Buildings.Add(new Building
        {
            Title = "Missile Silo",
            Description = "Stores interplantary missiles.",
            BaseMetalCost = 20000,
            BaseCrystalCost = 20000,
            BaseDeuteriumCost = 1000,    
            ConstructionDuration = TimeSpan.Parse("00:05:00"),
            Image = "building12.jpg",
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
            Image = "building13.jpg",
            EnergyConsumption = 100
        });
        
        StartProductionLoop();
    }

    private void StartProductionLoop()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1000); // Production tick every second
                CalculateProduction();
            }
        });
    }

    private void CalculateProduction()
    {
        // Simple formula: BaseProduction * Level * (SpeedFactor)
        // OGame formula is more complex: 30 * Level * 1.1^Level
        // We'll use a simplified linear scaling for now: 30 * Level per hour
        // Per second: (30 * Level) / 3600

        double metalProduction = 0;
        double crystalProduction = 0;
        double deuteriumProduction = 0;

        var metalMine = Buildings.FirstOrDefault(b => b.Title == "Metal Mine");
        if (metalMine != null && metalMine.Level > 0)
        {
            // Example: Level 1 = 30/hr = 0.0083/sec
            metalProduction = (30 * metalMine.Level * Math.Pow(1.1, metalMine.Level)) / 3600.0;
        }

        var crystalMine = Buildings.FirstOrDefault(b => b.Title == "Crystal Mine");
        if (crystalMine != null && crystalMine.Level > 0)
        {
            // Example: Level 1 = 20/hr
            crystalProduction = (20 * crystalMine.Level * Math.Pow(1.1, crystalMine.Level)) / 3600.0;
        }

        var deuteriumSynthesizer = Buildings.FirstOrDefault(b => b.Title == "Deuterium Synthesizer");
        if (deuteriumSynthesizer != null && deuteriumSynthesizer.Level > 0)
        {
            // Example: Level 1 = 10/hr
            deuteriumProduction = (10 * deuteriumSynthesizer.Level * Math.Pow(1.1, deuteriumSynthesizer.Level)) / 3600.0;
        }
        
        // Base production (always on even with 0 mines)
        metalProduction += 30.0 / 3600.0; 
        crystalProduction += 15.0 / 3600.0;

        _resourceService.AddResources(metalProduction, crystalProduction, deuteriumProduction);
    }

    public void AddToQueue(Building building)
    {
        if (ConstructionQueue.Count >= 5) return;
        
        if (_resourceService.HasResources(building.MetalCost, building.CrystalCost, building.DeuteriumCost))
        {
            _resourceService.ConsumeResources(building.MetalCost, building.CrystalCost, building.DeuteriumCost);
            
            // Set initial state for queue
            building.ConstructionDuration = building.Duration; // Reset or calculate
            building.TimeRemaining = building.ConstructionDuration;
            
            ConstructionQueue.Add(building);
            NotifyStateChanged();

            if (!_isProcessingQueue)
            {
                _ = ProcessQueue();
            }
        }
    }

    public void Cancel(Building building)
    {
        if (ConstructionQueue.Contains(building))
        {
            ConstructionQueue.Remove(building);
            building.IsBuilding = false;
            building.TimeRemaining = TimeSpan.Zero;
            // Optionally refund resources here
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
            
            // Energy update
            int oldEnergy = currentBuilding.EnergyConsumption * currentBuilding.Level;
            int newEnergy = currentBuilding.EnergyConsumption * (currentBuilding.Level + 1);
            int energyDiff = newEnergy - oldEnergy;
            _resourceService.UpdateEnergy(-energyDiff);

            currentBuilding.Level++;
            
            ConstructionQueue.RemoveAt(0);
            NotifyStateChanged();
        }

        _isProcessingQueue = false;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
