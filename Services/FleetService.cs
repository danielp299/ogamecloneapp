using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace myapp.Services;

public enum FleetStatus
{
    Flight,
    Return,
    Holding
}

public class FleetMission
{
    public Guid Id { get; set; }
    public string MissionType { get; set; }
    public string TargetCoordinates { get; set; }
    public Dictionary<string, int> Ships { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public DateTime ReturnTime { get; set; }
    public FleetStatus Status { get; set; }
    public long FuelConsumed { get; set; }
}

public class Ship
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
    public long Structure { get; set; } // Armor = Structure / 10
    public long Shield { get; set; }
    public long Attack { get; set; }
    public long Capacity { get; set; }
    public int BaseSpeed { get; set; }
    public long FuelConsumption { get; set; }
    
    // Construction
    public TimeSpan BaseDuration { get; set; }
    
    // Requirements (Simplified names for matching)
    public Dictionary<string, int> Requirements { get; set; } = new();
}

public class ShipyardItem
{
    public Ship Ship { get; set; }
    public int Quantity { get; set; }
    public TimeSpan DurationPerUnit { get; set; }
    public TimeSpan TimeRemaining { get; set; } // For the current unit being built
}

public class FleetService
{
    private readonly ResourceService _resourceService;
    private readonly BuildingService _buildingService;
    private readonly TechnologyService _technologyService;

    public List<Ship> ShipDefinitions { get; private set; } = new();
    
    // Inventory: ShipId -> Count
    public Dictionary<string, int> DockedShips { get; private set; } = new();
    
    // Shipyard Queue
    public List<ShipyardItem> ConstructionQueue { get; private set; } = new();

    // Active Fleets (Missions)
    public List<FleetMission> ActiveFleets { get; private set; } = new();

    public event Action OnChange;

    private bool _isProcessingQueue = false;

    public FleetService(ResourceService resourceService, BuildingService buildingService, TechnologyService technologyService)
    {
        _resourceService = resourceService;
        _buildingService = buildingService;
        _technologyService = technologyService;
        
        InitializeShips();
        
        // Start loops
        _ = ProcessQueueLoop();
        _ = ProcessFleetLoop();
    }

    private void InitializeShips()
    {
        ShipDefinitions.Add(new Ship
        {
            Id = "SC", Name = "Small Cargo", Description = "An agile transporter.",
            Image = "smallCargo.jpg",
            MetalCost = 2000, CrystalCost = 2000, DeuteriumCost = 0,
            Structure = 4000, Shield = 10, Attack = 5, Capacity = 5000, BaseSpeed = 5000, FuelConsumption = 10,
            BaseDuration = TimeSpan.FromSeconds(20),
            Requirements = new() { { "Shipyard", 2 }, { "Combustion Drive", 2 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "LC", Name = "Large Cargo", Description = "A heavy transporter with huge capacity.",
            Image = "largeCargo.jpg",
            MetalCost = 6000, CrystalCost = 6000, DeuteriumCost = 0,
            Structure = 12000, Shield = 25, Attack = 5, Capacity = 25000, BaseSpeed = 7500, FuelConsumption = 50,
            BaseDuration = TimeSpan.FromSeconds(50),
            Requirements = new() { { "Shipyard", 4 }, { "Combustion Drive", 6 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "LF", Name = "Light Fighter", Description = "The backbone of any fleet.",
            Image = "lightFighter.jpg",
            MetalCost = 3000, CrystalCost = 1000, DeuteriumCost = 0,
            Structure = 4000, Shield = 10, Attack = 50, Capacity = 50, BaseSpeed = 12500, FuelConsumption = 20,
            BaseDuration = TimeSpan.FromSeconds(15),
            Requirements = new() { { "Shipyard", 1 }, { "Combustion Drive", 1 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "HF", Name = "Heavy Fighter", Description = "Better armored than the light fighter.",
            Image = "heavyFighter.jpg",
            MetalCost = 6000, CrystalCost = 4000, DeuteriumCost = 0,
            Structure = 10000, Shield = 25, Attack = 150, Capacity = 100, BaseSpeed = 10000, FuelConsumption = 75,
            BaseDuration = TimeSpan.FromSeconds(40),
            Requirements = new() { { "Shipyard", 3 }, { "Impulse Drive", 2 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "CR", Name = "Cruiser", Description = "Fast and dangerous to fighters.",
            Image = "cruiser.jpg",
            MetalCost = 20000, CrystalCost = 7000, DeuteriumCost = 2000,
            Structure = 27000, Shield = 50, Attack = 400, Capacity = 800, BaseSpeed = 15000, FuelConsumption = 300,
            BaseDuration = TimeSpan.FromMinutes(2),
            Requirements = new() { { "Shipyard", 5 }, { "Impulse Drive", 4 }, { "Ion Technology", 2 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "BS", Name = "Battleship", Description = "The ruler of the battlefield.",
            Image = "battleship.jpg",
            MetalCost = 45000, CrystalCost = 15000, DeuteriumCost = 0,
            Structure = 60000, Shield = 200, Attack = 1000, Capacity = 1500, BaseSpeed = 10000, FuelConsumption = 500,
            BaseDuration = TimeSpan.FromMinutes(4),
            Requirements = new() { { "Shipyard", 7 }, { "Hyperspace Drive", 4 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "CS", Name = "Colony Ship", Description = "Used to colonize new planets.",
            Image = "colonyShip.jpg",
            MetalCost = 10000, CrystalCost = 20000, DeuteriumCost = 10000,
            Structure = 30000, Shield = 100, Attack = 50, Capacity = 7500, BaseSpeed = 2500, FuelConsumption = 1000,
            BaseDuration = TimeSpan.FromMinutes(5),
            Requirements = new() { { "Shipyard", 4 }, { "Impulse Drive", 3 } }
        });
        
        ShipDefinitions.Add(new Ship
        {
            Id = "REC", Name = "Recycler", Description = "Harvests debris fields.",
            Image = "recycler.jpg",
            MetalCost = 10000, CrystalCost = 6000, DeuteriumCost = 2000,
            Structure = 16000, Shield = 10, Attack = 1, Capacity = 20000, BaseSpeed = 2000, FuelConsumption = 300,
            BaseDuration = TimeSpan.FromMinutes(3),
            Requirements = new() { { "Shipyard", 4 }, { "Combustion Drive", 6 }, { "Shielding Technology", 2 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "ESP", Name = "Espionage Probe", Description = "Fast drone for spying.",
            Image = "probe.jpg",
            MetalCost = 0, CrystalCost = 1000, DeuteriumCost = 0,
            Structure = 1000, Shield = 0, Attack = 0, Capacity = 5, BaseSpeed = 100000000, FuelConsumption = 1,
            BaseDuration = TimeSpan.FromSeconds(5),
            Requirements = new() { { "Shipyard", 3 }, { "Combustion Drive", 3 }, { "Espionage Technology", 2 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "DST", Name = "Destroyer", Description = "Anti-Deathstar specialized ship.",
            Image = "destroyer.jpg",
            MetalCost = 60000, CrystalCost = 50000, DeuteriumCost = 15000,
            Structure = 110000, Shield = 500, Attack = 2000, Capacity = 2000, BaseSpeed = 5000, FuelConsumption = 1000,
            BaseDuration = TimeSpan.FromMinutes(10),
            Requirements = new() { { "Shipyard", 9 }, { "Hyperspace Drive", 6 }, { "Hyperspace Technology", 5 } }
        });
        
        ShipDefinitions.Add(new Ship
        {
            Id = "RIP", Name = "Death Star", Description = "The ultimate weapon.",
            Image = "deathstar.jpg",
            MetalCost = 5000000, CrystalCost = 4000000, DeuteriumCost = 1000000,
            Structure = 9000000, Shield = 50000, Attack = 200000, Capacity = 1000000, BaseSpeed = 100, FuelConsumption = 1,
            BaseDuration = TimeSpan.FromHours(5),
            Requirements = new() { { "Shipyard", 12 }, { "Hyperspace Drive", 7 }, { "Graviton Technology", 1 } }
        });
    }

    public int GetShipCount(string shipId)
    {
        return DockedShips.ContainsKey(shipId) ? DockedShips[shipId] : 0;
    }

    // --- Fleet Operations ---

    public long CalculateFuelConsumption(Dictionary<string, int> shipsToSend, int targetGalaxy, int targetSystem, int targetPosition)
    {
        // Placeholder distance: 1 System = 1000 units, 1 Galaxy = 20000 units
        // Current coordinates assumed 1:1:1 for simplicity
        long distance = Math.Abs(targetGalaxy - 1) * 20000 + Math.Abs(targetSystem - 1) * 1000 + Math.Abs(targetPosition - 1) * 5 + 1000;
        
        long totalFuel = 0;
        foreach(var kvp in shipsToSend)
        {
            var ship = ShipDefinitions.First(s => s.Id == kvp.Key);
            // Simple formula: Consumption * Distance / 1000 * Quantity
            totalFuel += (ship.FuelConsumption * distance / 1000) * kvp.Value;
        }
        
        return Math.Max(1, totalFuel);
    }

    public TimeSpan CalculateFlightTime(Dictionary<string, int> shipsToSend, int targetGalaxy, int targetSystem, int targetPosition)
    {
        if (!shipsToSend.Any()) return TimeSpan.Zero;
        
        // Find slowest ship
        int minSpeed = int.MaxValue;
        foreach(var kvp in shipsToSend)
        {
             var ship = ShipDefinitions.First(s => s.Id == kvp.Key);
             if (ship.BaseSpeed < minSpeed) minSpeed = ship.BaseSpeed;
        }
        
        // Distance
        long distance = Math.Abs(targetGalaxy - 1) * 20000 + Math.Abs(targetSystem - 1) * 1000 + Math.Abs(targetPosition - 1) * 5 + 1000;
        
        // Time = Distance / Speed * Factor (e.g. 100)
        // With x100 speed universe, we divide result by 100
        double hours = (double)distance / minSpeed;
        double seconds = hours * 3600 / 100.0; // x100 Speed universe
        
        if (seconds < 10) seconds = 10; // Minimum flight time

        return TimeSpan.FromSeconds(seconds);
    }

    public string SendFleet(Dictionary<string, int> shipsToSend, int g, int s, int p, string missionType)
    {
        if (!shipsToSend.Any()) return "No ships selected.";
        
        // 1. Check Ship Availability
        foreach(var kvp in shipsToSend)
        {
            if (GetShipCount(kvp.Key) < kvp.Value) return $"Not enough {kvp.Key}.";
        }
        
        // 2. Calculate Fuel & Check Deuterium
        long fuel = CalculateFuelConsumption(shipsToSend, g, s, p);
        if (!_resourceService.HasResources(0,0, fuel)) return "Not enough Deuterium for fuel.";
        
        // 3. Deduct Resources and Ships
        _resourceService.ConsumeResources(0,0, fuel);
        foreach(var kvp in shipsToSend)
        {
            DockedShips[kvp.Key] -= kvp.Value;
        }
        
        // 4. Create Mission
        var flightTime = CalculateFlightTime(shipsToSend, g, s, p);
        var mission = new FleetMission
        {
            Id = Guid.NewGuid(),
            MissionType = missionType,
            TargetCoordinates = $"{g}:{s}:{p}",
            Ships = new Dictionary<string, int>(shipsToSend),
            StartTime = DateTime.Now,
            ArrivalTime = DateTime.Now.Add(flightTime),
            ReturnTime = DateTime.Now.Add(flightTime).Add(flightTime), // Simple return logic
            Status = FleetStatus.Flight,
            FuelConsumed = fuel
        };
        
        ActiveFleets.Add(mission);
        NotifyStateChanged();
        
        return null; // Success
    }

    private async Task ProcessFleetLoop()
    {
        while (true)
        {
            var now = DateTime.Now;
            var completedMissions = new List<FleetMission>();
            
            // Iterate backwards or copy list to modify
            foreach (var mission in ActiveFleets.ToList())
            {
                if (mission.Status == FleetStatus.Flight)
                {
                    if (now >= mission.ArrivalTime)
                    {
                        // Arrived!
                        // Logic: Process Mission (Combat, Transport, etc)
                        // For now: Just turn around
                        mission.Status = FleetStatus.Return;
                        NotifyStateChanged();
                    }
                }
                else if (mission.Status == FleetStatus.Return)
                {
                    if (now >= mission.ReturnTime)
                    {
                        // Returned to base
                        foreach(var kvp in mission.Ships)
                        {
                            if (!DockedShips.ContainsKey(kvp.Key)) DockedShips[kvp.Key] = 0;
                            DockedShips[kvp.Key] += kvp.Value;
                        }
                        
                        completedMissions.Add(mission);
                        NotifyStateChanged();
                    }
                }
            }
            
            if (completedMissions.Any())
            {
                foreach(var m in completedMissions) ActiveFleets.Remove(m);
                NotifyStateChanged();
            }
            
            await Task.Delay(1000);
        }
    }

    public void AddToQueue(Ship ship, int quantity)
    {
        if (quantity <= 0) return;

        long totalMetal = ship.MetalCost * quantity;
        long totalCrystal = ship.CrystalCost * quantity;
        long totalDeuterium = ship.DeuteriumCost * quantity;

        if (_resourceService.HasResources(totalMetal, totalCrystal, totalDeuterium))
        {
            _resourceService.ConsumeResources(totalMetal, totalCrystal, totalDeuterium);
            
            // Calculate duration based on Shipyard and Nanite levels
            // Formula: Duration = Base / (1 + Shipyard) * 2^Nanite * SpeedFactor
            var shipyardLevel = _buildingService.GetBuildingLevel("Shipyard");
            var naniteLevel = _buildingService.GetBuildingLevel("Nanite Factory");
            
            // Avoid division by zero if shipyard is somehow 0 (though requirements check should prevent this)
            double divisor = (1 + shipyardLevel) * Math.Pow(2, naniteLevel);
            if (divisor < 1) divisor = 1;

            // Apply building speed multiplier from BuildingService for consistency/testing
            double durationSeconds = ship.BaseDuration.TotalSeconds / divisor / 100.0; // Keeping x100 speed
            if (durationSeconds < 1) durationSeconds = 1; // Minimum 1 second

            ConstructionQueue.Add(new ShipyardItem
            {
                Ship = ship,
                Quantity = quantity,
                DurationPerUnit = TimeSpan.FromSeconds(durationSeconds),
                TimeRemaining = TimeSpan.FromSeconds(durationSeconds)
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
                
                // Process current unit
                currentItem.TimeRemaining = currentItem.TimeRemaining.Subtract(TimeSpan.FromSeconds(1));

                if (currentItem.TimeRemaining <= TimeSpan.Zero)
                {
                    // Unit complete
                    if (!DockedShips.ContainsKey(currentItem.Ship.Id))
                        DockedShips[currentItem.Ship.Id] = 0;
                    
                    DockedShips[currentItem.Ship.Id]++;
                    
                    currentItem.Quantity--;
                    
                    if (currentItem.Quantity > 0)
                    {
                        // Reset timer for next unit
                        currentItem.TimeRemaining = currentItem.DurationPerUnit;
                    }
                    else
                    {
                        // Batch complete
                        ConstructionQueue.RemoveAt(0);
                    }
                    
                    NotifyStateChanged();
                }
                NotifyStateChanged(); // Update timer UI
            }
            
            await Task.Delay(1000); // 1 second tick
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
