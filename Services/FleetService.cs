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
    public Dictionary<string, long> Cargo { get; set; } = new();
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
    private readonly GalaxyService _galaxyService;
    private readonly MessageService _messageService;
    private readonly DefenseService _defenseService;
    private readonly DevModeService _devModeService;

    public List<Ship> ShipDefinitions { get; private set; } = new();
    
    // Inventory: ShipId -> Count
    public Dictionary<string, int> DockedShips { get; private set; } = new();
    
    // Shipyard Queue
    public List<ShipyardItem> ConstructionQueue { get; private set; } = new();

    // Active Fleets (Missions)
    public List<FleetMission> ActiveFleets { get; private set; } = new();

    public event Action OnChange;

    private bool _isProcessingQueue = false;

    public FleetService(ResourceService resourceService, BuildingService buildingService, TechnologyService technologyService, GalaxyService galaxyService, MessageService messageService, DefenseService defenseService, DevModeService devModeService)
    {
        _resourceService = resourceService;
        _buildingService = buildingService;
        _technologyService = technologyService;
        _galaxyService = galaxyService;
        _messageService = messageService;
        _defenseService = defenseService;
        _devModeService = devModeService;
        
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
        
        // Reduce attack time by 90%
        seconds = seconds * 0.1;
        
        if (seconds < 2) seconds = 2; // Minimum flight time

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
        flightTime = _devModeService.GetDuration(flightTime, 10); // Dev mode override

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
                        ProcessMissionArrival(mission);
                        
                        // Turn around
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

                        // Unload Cargo
                        if (mission.Cargo.Count > 0)
                        {
                            long m = mission.Cargo.ContainsKey("Metal") ? mission.Cargo["Metal"] : 0;
                            long c = mission.Cargo.ContainsKey("Crystal") ? mission.Cargo["Crystal"] : 0;
                            long d = mission.Cargo.ContainsKey("Deuterium") ? mission.Cargo["Deuterium"] : 0;
                            _resourceService.AddResources(m, c, d);
                        }
                        
                        _messageService.AddMessage("Fleet Return", 
                            $"Your fleet from {mission.TargetCoordinates} has returned to base.", 
                            "General");
                        
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

    private void ProcessMissionArrival(FleetMission mission)
    {
        // Parse coords
        var parts = mission.TargetCoordinates.Split(':');
        int g = int.Parse(parts[0]);
        int s = int.Parse(parts[1]);
        int p = int.Parse(parts[2]);

        // Get target info
        var system = _galaxyService.GetSystem(g, s);
        var planet = system.FirstOrDefault(pl => pl.Position == p);
        
        // Handle Missions
        switch (mission.MissionType)
        {
            case "Espionage":
                HandleEspionage(mission, planet);
                break;
            case "Attack":
                HandleCombat(mission, planet);
                break;
            case "Colonize":
                HandleColonization(mission, planet);
                break;
            case "Recycle":
                HandleRecycle(mission, planet);
                break;
            case "Expedition":
                HandleExpedition(mission);
                break;
            default:
                 _messageService.AddMessage("Fleet Reached Destination", 
                     $"Your fleet arrived at {mission.TargetCoordinates} and is returning.", "General");
                break;
        }
    }

    private void HandleExpedition(FleetMission mission)
    {
        var random = new Random();
        int outcome = random.Next(1, 101); // 1 to 100

        // Determine Fleet Capacity
        long totalCapacity = 0;
        foreach (var s in mission.Ships)
        {
            var def = ShipDefinitions.FirstOrDefault(x => x.Id == s.Key);
            if (def != null) totalCapacity += def.Capacity * s.Value;
        }

        if (outcome <= 30) // 30% Chance: Find Resources
        {
            long foundMetal = random.Next(1000, 50000);
            long foundCrystal = random.Next(500, 25000);
            long foundDeuterium = random.Next(0, 10000);

            // Cap at 50% capacity or full capacity, whichever is smaller logic could be here, 
            // but let's just cap at remaining capacity
            long currentLoad = mission.Cargo.Values.Sum();
            long available = totalCapacity - currentLoad;

            if (available > 0)
            {
                long totalFound = foundMetal + foundCrystal + foundDeuterium;
                if (totalFound > available)
                {
                    double ratio = (double)available / totalFound;
                    foundMetal = (long)(foundMetal * ratio);
                    foundCrystal = (long)(foundCrystal * ratio);
                    foundDeuterium = (long)(foundDeuterium * ratio);
                }

                if (!mission.Cargo.ContainsKey("Metal")) mission.Cargo["Metal"] = 0;
                mission.Cargo["Metal"] += foundMetal;

                if (!mission.Cargo.ContainsKey("Crystal")) mission.Cargo["Crystal"] = 0;
                mission.Cargo["Crystal"] += foundCrystal;

                if (!mission.Cargo.ContainsKey("Deuterium")) mission.Cargo["Deuterium"] = 0;
                mission.Cargo["Deuterium"] += foundDeuterium;

                _messageService.AddMessage("Expedition Result", 
                    $"Your expedition found a resource pocket!\n" +
                    $"Metal: {foundMetal:N0}, Crystal: {foundCrystal:N0}, Deuterium: {foundDeuterium:N0}", "Expedition");
            }
            else
            {
                 _messageService.AddMessage("Expedition Result", 
                    "Your expedition found resources, but your cargo holds were full!", "Expedition");
            }
        }
        else if (outcome <= 50) // 20% Chance: Find Ships
        {
            // Find a few Light Fighters or Small Cargo
            int shipType = random.Next(0, 2); 
            string foundId = shipType == 0 ? "LF" : "SC";
            string foundName = shipType == 0 ? "Light Fighter" : "Small Cargo";
            int count = random.Next(2, 10);

            if (!mission.Ships.ContainsKey(foundId)) mission.Ships[foundId] = 0;
            mission.Ships[foundId] += count;

            _messageService.AddMessage("Expedition Result", 
                $"Your expedition encountered abandoned ships.\n" +
                $"They managed to repair {count} {foundName}(s) and added them to the fleet.", "Expedition");
        }
        else if (outcome <= 60) // 10% Chance: Pirates/Aliens (Combat)
        {
            // Minimal implementation: Just damage/loss message for now to keep it simple
            // In a full implementation, this would trigger a combat round
            
            // Lose 1 random ship
            var keys = mission.Ships.Keys.ToList();
            if (keys.Count > 0)
            {
                string keyToLose = keys[random.Next(keys.Count)];
                if (mission.Ships[keyToLose] > 0)
                {
                    mission.Ships[keyToLose]--;
                    if (mission.Ships[keyToLose] <= 0) mission.Ships.Remove(keyToLose);
                    
                    var lostShipName = ShipDefinitions.FirstOrDefault(s => s.Id == keyToLose)?.Name ?? keyToLose;

                    _messageService.AddMessage("Expedition Result", 
                        $"Your expedition was attacked by pirates! You fought them off, but lost 1 {lostShipName}.", "Expedition");
                }
            }
            else
            {
                 _messageService.AddMessage("Expedition Result", 
                    "Your expedition was attacked by pirates! You escaped without losses.", "Expedition");
            }
        }
        else if (outcome <= 62) // 2% Chance: Black Hole (Total Loss)
        {
            // Clear ships
            mission.Ships.Clear();
            // This will likely cause issues if the fleet logic expects ships to return
            // We need to handle 'fleet destruction' properly.
            // If ships is empty, the return logic might just credit 0 ships, which is fine.
            
             _messageService.AddMessage("Expedition Result", 
                "Your expedition encountered a Black Hole and was never heard from again.", "Expedition");
        }
        else // 38% Chance: Nothing
        {
            string[] flavorText = {
                "The expedition found nothing but empty space.",
                "Strange signals were detected, but they turned out to be background radiation.",
                "The crew enjoyed a nice view of a supernova, but found nothing of value.",
                "Main sensors malfunctioned for a while. Nothing to report."
            };
            string text = flavorText[random.Next(flavorText.Length)];
            
            _messageService.AddMessage("Expedition Result", text, "Expedition");
        }
    }

    private void HandleEspionage(FleetMission mission, GalaxyPlanet planet)
    {
        if (planet == null || !planet.IsOccupied)
        {
             _messageService.AddMessage("Espionage Report", 
                 $"Sector {mission.TargetCoordinates} is empty.", "Espionage");
             return;
        }

        // Generate consistent resources/defense based on planet position/ID
        var (resources, defenses) = GeneratePlanetState(planet);

        long m = resources["Metal"];
        long c = resources["Crystal"];
        long d = resources["Deuterium"];
        
        string defenseRows = "";
        foreach(var def in defenses)
        {
            if(def.Value > 0)
                defenseRows += $"{def.Key}: {def.Value:N0}<br/>";
        }
        if (string.IsNullOrEmpty(defenseRows)) defenseRows = "None";

        string body = $@"
            <strong>Target:</strong> {planet.Name} ({planet.PlayerName})<br/>
            <strong>Resources:</strong><br/>
            Metal: {m:N0}<br/>
            Crystal: {c:N0}<br/>
            Deuterium: {d:N0}<br/>
            <br/>
            <strong>Defense:</strong><br/>
            {defenseRows}
        ";
        
        _messageService.AddMessage($"Spy Report [{mission.TargetCoordinates}]", body, "Espionage");
    }

    private void HandleCombat(FleetMission mission, GalaxyPlanet planet)
    {
        if (!planet.IsOccupied)
        {
            _messageService.AddMessage("Combat Report", "Planet is uninhabited. No combat occurred.", "Combat");
            return;
        }
        
        if (planet.IsMyPlanet)
        {
             _messageService.AddMessage("Combat Report", "You cannot attack your own planet.", "Combat");
             return;
        }

        // 1. Attacker Power
        long attackerAttack = 0;
        long attackerStructure = 0;
        long attackerShield = 0;

        foreach(var s in mission.Ships)
        {
             var def = ShipDefinitions.FirstOrDefault(x => x.Id == s.Key);
             if(def != null) 
             {
                 attackerAttack += def.Attack * s.Value;
                 attackerStructure += def.Structure * s.Value;
                 attackerShield += def.Shield * s.Value;
             }
        }
        
        // 2. Defender Power (Generated)
        var (resources, defenses) = GeneratePlanetState(planet);
        
        long defenderAttack = 0;
        long defenderStructure = 0;
        long defenderShield = 0;
        
        foreach(var d in defenses)
        {
            // Try to match name to Definition for accurate stats
            var defUnit = _defenseService.DefenseDefinitions.FirstOrDefault(u => u.Name == d.Key);
            
            if (defUnit != null)
            {
                 defenderAttack += defUnit.Attack * d.Value;
                 defenderStructure += defUnit.Structure * d.Value;
                 defenderShield += defUnit.Shield * d.Value;
            }
            else
            {
                // Fallback if name mismatch (e.g. from simplistic generator vs complex names)
                // We'll keep the simplistic heuristic as backup
                if(d.Key.Contains("Rocket")) { defenderAttack += 80 * d.Value; defenderStructure += 2000 * d.Value; defenderShield += 20 * d.Value; }
                else if(d.Key.Contains("Laser")) { defenderAttack += 100 * d.Value; defenderStructure += 2000 * d.Value; defenderShield += 25 * d.Value; }
                else if(d.Key.Contains("Heavy") || d.Key.Contains("Cannon")) { defenderAttack += 250 * d.Value; defenderStructure += 8000 * d.Value; defenderShield += 100 * d.Value; }
                else { defenderAttack += 50 * d.Value; defenderStructure += 1000 * d.Value; defenderShield += 10 * d.Value; }
            }
        }

        // 3. Battle Resolution (Simplified)
        // Total Health = Structure + Shield
        long attackerHealth = attackerStructure + attackerShield;
        long defenderHealth = defenderStructure + defenderShield;
        
        // Avoid div by zero
        if(attackerHealth <= 0) attackerHealth = 1;
        if(defenderHealth <= 0) defenderHealth = 1;

        // Rounds? Or simple comparison?
        // Simple: Higher damage output relative to enemy health wins.
        // Score = Attack / EnemyHealth
        double attackerScore = (double)attackerAttack / defenderHealth;
        double defenderScore = (double)defenderAttack / attackerHealth;
        
        string result = "";
        
        if (attackerScore > defenderScore)
        {
            // WIN
            long lootM = Math.Min(resources["Metal"] / 2, mission.Cargo.ContainsKey("Metal") ? 0 : 1000000); // 50% loot
            long lootC = Math.Min(resources["Crystal"] / 2, 1000000);
            long lootD = Math.Min(resources["Deuterium"] / 2, 1000000);
            
            // Cap by capacity
            long totalCapacity = 0;
            foreach(var s in mission.Ships) {
                 var def = ShipDefinitions.FirstOrDefault(x => x.Id == s.Key);
                 if(def!=null) totalCapacity += def.Capacity * s.Value;
            }
            
            long totalLoot = lootM + lootC + lootD;
            if(totalLoot > totalCapacity)
            {
                double ratio = (double)totalCapacity / totalLoot;
                lootM = (long)(lootM * ratio);
                lootC = (long)(lootC * ratio);
                lootD = (long)(lootD * ratio);
            }

            // Add to Cargo
            if (!mission.Cargo.ContainsKey("Metal")) mission.Cargo["Metal"] = 0;
            mission.Cargo["Metal"] += lootM;

            if (!mission.Cargo.ContainsKey("Crystal")) mission.Cargo["Crystal"] = 0;
            mission.Cargo["Crystal"] += lootC;
            
            if (!mission.Cargo.ContainsKey("Deuterium")) mission.Cargo["Deuterium"] = 0;
            mission.Cargo["Deuterium"] += lootD;

            // Generate Debris from destroyed Defenses (30% of cost)
            long debrisM = 0;
            long debrisC = 0;
            foreach(var d in defenses)
            {
                var defUnit = _defenseService.DefenseDefinitions.FirstOrDefault(u => u.Name == d.Key);
                // Fallback for generic names if needed, but defenses dict keys should match names if generated correctly
                // or we use the fallback stats from earlier. 
                // For simplified debris, let's try to find the definition or estimate.
                
                long unitMetal = 0;
                long unitCrystal = 0;

                if (defUnit != null)
                {
                    unitMetal = defUnit.MetalCost;
                    unitCrystal = defUnit.CrystalCost;
                }
                else
                {
                     // Estimation for "Bots" if exact match fails
                     if(d.Key.Contains("Rocket")) { unitMetal = 2000; }
                     else if(d.Key.Contains("Laser")) { unitMetal = 1500; unitCrystal = 500; }
                     else { unitMetal = 1000; }
                }

                debrisM += (long)(unitMetal * d.Value * 0.3);
                debrisC += (long)(unitCrystal * d.Value * 0.3);
            }
            
            if (debrisM > 0 || debrisC > 0)
            {
                planet.DebrisMetal += debrisM;
                planet.DebrisCrystal += debrisC;
                planet.HasDebris = true;
            }
            
            result = $"<span style='color:green'>VICTORY!</span><br/>" +
                     $"Your Fleet: {attackerAttack:N0} Atk / {attackerHealth:N0} HP<br/>" +
                     $"Enemy Def: {defenderAttack:N0} Atk / {defenderHealth:N0} HP<br/><br/>" +
                     $"Loot captured: <br/>" +
                     $"Metal: {lootM:N0}<br/>" +
                     $"Crystal: {lootC:N0}<br/>" +
                     $"Deuterium: {lootD:N0}<br/><br/>" +
                     $"Debris Field Created:<br/>" +
                     $"Metal: {debrisM:N0}, Crystal: {debrisC:N0}";
        }
        else
        {
            // DEFEAT
             
             // Destroy 50% of ships
             long debrisM = 0;
             long debrisC = 0;
             
             foreach(var key in mission.Ships.Keys.ToList())
             {
                 int original = mission.Ships[key];
                 int lost = original / 2; // Lose half
                 mission.Ships[key] -= lost; // Update fleet
                 
                 var ship = ShipDefinitions.FirstOrDefault(s => s.Id == key);
                 if (ship != null)
                 {
                     debrisM += (long)(ship.MetalCost * lost * 0.3);
                     debrisC += (long)(ship.CrystalCost * lost * 0.3);
                 }
             }

             if (debrisM > 0 || debrisC > 0)
             {
                planet.DebrisMetal += debrisM;
                planet.DebrisCrystal += debrisC;
                planet.HasDebris = true;
             }

             result = $"<span style='color:red'>DEFEAT!</span><br/>" +
                     $"Your Fleet: {attackerAttack:N0} Atk / {attackerHealth:N0} HP<br/>" +
                     $"Enemy Def: {defenderAttack:N0} Atk / {defenderHealth:N0} HP<br/><br/>" +
                     $"Your fleet was forced to retreat with heavy losses.<br/><br/>" +
                     $"Debris Field Created:<br/>" +
                     $"Metal: {debrisM:N0}, Crystal: {debrisC:N0}";
        }
        
        _messageService.AddMessage($"Combat Report [{mission.TargetCoordinates}]", result, "Combat");
    }

    // Helper to generate consistent state for a planet based on its coordinate
    private (Dictionary<string, long> Resources, Dictionary<string, int> Defenses) GeneratePlanetState(GalaxyPlanet planet)
    {
        // Seed random with position to get same result for same planet
        int seed = planet.Position * 1000 + planet.Name.Length * 7; 
        // We really need Galaxy/System info here to be unique, but Planet object might not have it fully populated in this context if passed purely as object.
        // Assuming unique enough for this demo.
        var r = new Random(seed);
        
        var resources = new Dictionary<string, long>
        {
            { "Metal", r.Next(5000, 100000) },
            { "Crystal", r.Next(2000, 50000) },
            { "Deuterium", r.Next(0, 20000) }
        };

        var defenses = new Dictionary<string, int>
        {
            { "Rocket Launcher", r.Next(0, 50) },
            { "Light Laser", r.Next(0, 30) },
            { "Heavy Cannon", r.Next(0, 10) }
        };

        return (resources, defenses);
    }

    private void HandleColonization(FleetMission mission, GalaxyPlanet planet)
    {
        if (planet.IsOccupied)
        {
            _messageService.AddMessage("Colonization Failed", 
                 $"Planet {mission.TargetCoordinates} is already occupied!", "General");
             return;
        }

        // Check Astrophysics Limit
        int astroLevel = _technologyService.GetTechLevel(TechType.Astrophysics);
        int maxPlanets = 1 + (astroLevel + 1) / 2;

        if (_galaxyService.PlayerPlanets.Count >= maxPlanets)
        {
            _messageService.AddMessage("Colonization Failed", 
                 $"Astrophysics Level {astroLevel} allows only {maxPlanets} planets. Upgrade Astrophysics to colonize more.", "General");
             return;
        }

        // Check for Colony Ship
        if (!mission.Ships.ContainsKey("CS") || mission.Ships["CS"] < 1)
        {
            _messageService.AddMessage("Colonization Failed", 
                 "No Colony Ship present in the fleet.", "General");
             return;
        }

        // Consume Colony Ship
        mission.Ships["CS"]--;
        if (mission.Ships["CS"] <= 0) mission.Ships.Remove("CS");
        
        // Update Galaxy
        planet.IsOccupied = true;
        planet.IsMyPlanet = true;
        planet.Name = "Colony";
        planet.PlayerName = "Commander";
        planet.Image = "planet_colony.jpg";
        
        _galaxyService.RegisterPlanet(planet);

        _messageService.AddMessage("Colonization Successful", 
            $"You have successfully colonized position {mission.TargetCoordinates}!", "General");
    }

    private void HandleRecycle(FleetMission mission, GalaxyPlanet planet)
    {
        if (!planet.HasDebris)
        {
            _messageService.AddMessage("Recycle Report", 
                 $"No debris found at {mission.TargetCoordinates}.", "General");
             return;
        }
        
        // Calculate Capacity
        long capacity = 0;
        foreach(var s in mission.Ships)
        {
            var def = ShipDefinitions.FirstOrDefault(x => x.Id == s.Key);
            if (def != null && def.Id == "REC") // Only recyclers works effectively? For now any ship can carry
            {
                capacity += def.Capacity * s.Value;
            }
            else if(def != null)
            {
                 // Normal ships can assume some cargo? Let's say yes for simplicity
                 capacity += def.Capacity * s.Value;
            }
        }
        
        long gatheredMetal = Math.Min(planet.DebrisMetal, capacity);
        capacity -= gatheredMetal;
        long gatheredCrystal = Math.Min(planet.DebrisCrystal, capacity);
        
        // Update Debris
        planet.DebrisMetal -= gatheredMetal;
        planet.DebrisCrystal -= gatheredCrystal;
        if (planet.DebrisMetal <= 0 && planet.DebrisCrystal <= 0) planet.HasDebris = false;
        
        // Add to Fleet Cargo
        if (!mission.Cargo.ContainsKey("Metal")) mission.Cargo["Metal"] = 0;
        mission.Cargo["Metal"] += gatheredMetal;

        if (!mission.Cargo.ContainsKey("Crystal")) mission.Cargo["Crystal"] = 0;
        mission.Cargo["Crystal"] += gatheredCrystal;

        _messageService.AddMessage("Harvest Report", 
            $"Harvested {gatheredMetal:N0} Metal and {gatheredCrystal:N0} Crystal from debris field.", "General");
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

            var finalDuration = TimeSpan.FromSeconds(durationSeconds);
            finalDuration = _devModeService.GetDuration(finalDuration, 5); // Dev override

            ConstructionQueue.Add(new ShipyardItem
            {
                Ship = ship,
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
