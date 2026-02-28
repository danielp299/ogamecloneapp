using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

// Refer to wiki/business-rules/Fleet.md for business rules documentation
// Refer to wiki/business-rules/Combat.md for combat mechanics
// Refer to wiki/business-rules/Factory.md for shipyard and ship construction rules

public enum FleetStatus
{
    Flight,
    Return,
    Holding
}

public class FleetMission
{
    public Guid Id { get; set; }
    public string MissionType { get; set; } = "";
    public string OriginCoordinates { get; set; } = "";
    public string TargetCoordinates { get; set; } = "";
    public Dictionary<string, int> Ships { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public DateTime ReturnTime { get; set; }
    public FleetStatus Status { get; set; }
    public long FuelConsumed { get; set; }
    public Dictionary<string, long> Cargo { get; set; } = new();
    public bool IsIncomingAttack { get; set; }
    
    // Calculated properties for countdown
    public TimeSpan TimeToArrival => Status == FleetStatus.Flight && ArrivalTime > DateTime.Now 
        ? ArrivalTime - DateTime.Now 
        : TimeSpan.Zero;
    
    public TimeSpan TimeToReturn => Status == FleetStatus.Return && ReturnTime > DateTime.Now 
        ? ReturnTime - DateTime.Now 
        : TimeSpan.Zero;
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
    
    // Mission Capabilities
    public bool IsEspionageCapable { get; set; }
    public bool IsColonizationCapable { get; set; }
    public bool IsRecyclingCapable { get; set; }
}

public class ShipyardItem
{
    public Ship Ship { get; set; } = null!;
    public int Quantity { get; set; }
    public TimeSpan DurationPerUnit { get; set; }
    public TimeSpan TimeRemaining { get; set; } // For the current unit being built
}

// Refer to wiki/business-rules/Fleet.md for business rules documentation (includes combat logic)

public class FleetService
{
    private readonly GameDbContext _dbContext;
    private readonly ResourceService _resourceService;
    private readonly BuildingService _buildingService;
    private readonly TechnologyService _technologyService;
    private readonly GalaxyService _galaxyService;
    private readonly GamePersistenceService _persistenceService;
    private readonly PlayerStateService _playerStateService;
    private readonly MessageService _messageService;
    private readonly DefenseService _defenseService;
    private readonly DevModeService _devModeService;
    private readonly EnemyService _enemyService;

    public List<Ship> ShipDefinitions { get; private set; } = new();
    
    // Inventory: ShipId -> Count
    public Dictionary<string, int> DockedShips { get; private set; } = new();
    
    // Shipyard Queue
    public List<ShipyardItem> ConstructionQueue { get; private set; } = new();

    // Active Fleets (Missions)
    public List<FleetMission> ActiveFleets { get; private set; } = new();

    public event Action? OnChange;

    private bool _isProcessingQueue = false;
    private bool _isInitialized = false;
    
    // Maximum number of planets a player can have (colonization limit)
    public const int MaxPlanets = 4;
    public double CombatDefenseLossMinPercentage { get; private set; } = 0.10;
    public double CombatDefenseLossMaxPercentage { get; private set; } = 0.25;

    public FleetService(GameDbContext dbContext, ResourceService resourceService, BuildingService buildingService, TechnologyService technologyService, GalaxyService galaxyService, GamePersistenceService persistenceService, MessageService messageService, DefenseService defenseService, DevModeService devModeService, EnemyService enemyService, PlayerStateService playerStateService)
    {
        _dbContext = dbContext;
        _resourceService = resourceService;
        _buildingService = buildingService;
        _technologyService = technologyService;
        _galaxyService = galaxyService;
        _persistenceService = persistenceService;
        _messageService = messageService;
        _defenseService = defenseService;
        _devModeService = devModeService;
        _enemyService = enemyService;
        _playerStateService = playerStateService;
        
        InitializeShips();

        _playerStateService.OnChange += async () => 
        {
            await LoadFromDatabaseAsync();
            NotifyStateChanged();
        };

        _enemyService.OnEnemyAttackLaunched += HandleEnemyAttackLaunched;
        
        // DevMode: Add 10 ships of each type
        if (_devModeService.IsEnabled)
        {
            AddDevModeShips();
        }
        
        // Start loops
        _ = ProcessQueueLoop();
        _ = ProcessFleetLoop();
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await LoadFromDatabaseAsync();
        _isInitialized = true;
    }

    public void SetCombatDefenseLossPercentage(double percentage)
    {
        double clamped = Math.Clamp(percentage, 0.0, 1.0);
        CombatDefenseLossMinPercentage = clamped;
        CombatDefenseLossMaxPercentage = clamped;
    }

    public void SetCombatDefenseLossRange(double minPercentage, double maxPercentage)
    {
        double clampedMin = Math.Clamp(minPercentage, 0.0, 1.0);
        double clampedMax = Math.Clamp(maxPercentage, 0.0, 1.0);

        if (clampedMax < clampedMin)
        {
            (clampedMin, clampedMax) = (clampedMax, clampedMin);
        }

        CombatDefenseLossMinPercentage = clampedMin;
        CombatDefenseLossMaxPercentage = clampedMax;
    }

    public bool HasAttackInProgress()
    {
        return ActiveFleets.Any(m => m.MissionType == "Attack" && m.Status == FleetStatus.Flight && !m.IsIncomingAttack);
    }

    public bool HasIncomingAttack()
    {
        return ActiveFleets.Any(m => m.MissionType == "Attack" && m.Status == FleetStatus.Flight && m.IsIncomingAttack);
    }

    private async Task LoadFromDatabaseAsync()
    {
        int g = _playerStateService.ActiveGalaxy;
        int s = _playerStateService.ActiveSystem;
        int p = _playerStateService.ActivePosition;

        var dbShips = await _dbContext.Ships
            .Where(sh => sh.Galaxy == g && sh.System == s && sh.Position == p)
            .ToListAsync();
        
        // Reset counts
        foreach (var ship in ShipDefinitions) DockedShips[ship.Id] = 0;

        foreach (var dbShip in dbShips)
        {
            DockedShips[dbShip.ShipType] = dbShip.Quantity;
        }
    }

    private async Task SaveToDatabaseAsync()
    {
        int g = _playerStateService.ActiveGalaxy;
        int s = _playerStateService.ActiveSystem;
        int p = _playerStateService.ActivePosition;

        foreach (var kvp in DockedShips)
        {
            var dbShip = await _dbContext.Ships.FirstOrDefaultAsync(sh => 
                sh.ShipType == kvp.Key && sh.Galaxy == g && sh.System == s && sh.Position == p);
            
            if (dbShip != null)
            {
                dbShip.Quantity = kvp.Value;
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    private async Task SaveShipsToPlanetAsync(int g, int s, int p, Dictionary<string, int> ships)
    {
        foreach (var kvp in ships)
        {
            var dbShip = await _dbContext.Ships.FirstOrDefaultAsync(sh => sh.ShipType == kvp.Key && sh.Galaxy == g && sh.System == s && sh.Position == p);
            if (dbShip != null)
            {
                dbShip.Quantity += kvp.Value;
            }
            else
            {
                _dbContext.Ships.Add(new ShipEntity
                {
                    ShipType = kvp.Key,
                    Quantity = kvp.Value,
                    Galaxy = g,
                    System = s,
                    Position = p
                });
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    private void InitializeShips()
    {
        ShipDefinitions.Add(new Ship
        {
            Id = "SC", Name = "Small Cargo", Description = "An agile transporter.",
            Image = "assets/ships/smallCargo.jpg",
            MetalCost = 2000, CrystalCost = 2000, DeuteriumCost = 0,
            Structure = 4000, Shield = 10, Attack = 5, Capacity = 5000, BaseSpeed = 5000, FuelConsumption = 10,
            BaseDuration = TimeSpan.FromSeconds(20),
            Requirements = new() { { "Shipyard", 2 }, { "Combustion Drive", 2 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "LC", Name = "Large Cargo", Description = "A heavy transporter with huge capacity.",
            Image = "assets/ships/largeCargo.jpg",
            MetalCost = 6000, CrystalCost = 6000, DeuteriumCost = 0,
            Structure = 12000, Shield = 25, Attack = 5, Capacity = 25000, BaseSpeed = 7500, FuelConsumption = 50,
            BaseDuration = TimeSpan.FromSeconds(50),
            Requirements = new() { { "Shipyard", 4 }, { "Combustion Drive", 6 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "LF", Name = "Light Fighter", Description = "The backbone of any fleet.",
            Image = "assets/ships/lightFighter.jpg",
            MetalCost = 3000, CrystalCost = 1000, DeuteriumCost = 0,
            Structure = 4000, Shield = 10, Attack = 50, Capacity = 50, BaseSpeed = 12500, FuelConsumption = 20,
            BaseDuration = TimeSpan.FromSeconds(15),
            Requirements = new() { { "Shipyard", 1 }, { "Combustion Drive", 1 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "HF", Name = "Heavy Fighter", Description = "Better armored than the light fighter.",
            Image = "assets/ships/heavyFighter.jpg",
            MetalCost = 6000, CrystalCost = 4000, DeuteriumCost = 0,
            Structure = 10000, Shield = 25, Attack = 150, Capacity = 100, BaseSpeed = 10000, FuelConsumption = 75,
            BaseDuration = TimeSpan.FromSeconds(40),
            Requirements = new() { { "Shipyard", 3 }, { "Impulse Drive", 2 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "CR", Name = "Cruiser", Description = "Fast and dangerous to fighters.",
            Image = "assets/ships/cruiser.jpg",
            MetalCost = 20000, CrystalCost = 7000, DeuteriumCost = 2000,
            Structure = 27000, Shield = 50, Attack = 400, Capacity = 800, BaseSpeed = 15000, FuelConsumption = 300,
            BaseDuration = TimeSpan.FromMinutes(2),
            Requirements = new() { { "Shipyard", 5 }, { "Impulse Drive", 4 }, { "Ion Technology", 2 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "BS", Name = "Battleship", Description = "The ruler of the battlefield.",
            Image = "assets/ships/battleship.jpg",
            MetalCost = 45000, CrystalCost = 15000, DeuteriumCost = 0,
            Structure = 60000, Shield = 200, Attack = 1000, Capacity = 1500, BaseSpeed = 10000, FuelConsumption = 500,
            BaseDuration = TimeSpan.FromMinutes(4),
            Requirements = new() { { "Shipyard", 7 }, { "Hyperspace Drive", 4 } }
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "CS", Name = "Colony Ship", Description = "Used to colonize new planets.",
            Image = "assets/ships/colonyShip.jpg",
            MetalCost = 10000, CrystalCost = 20000, DeuteriumCost = 10000,
            Structure = 30000, Shield = 100, Attack = 50, Capacity = 7500, BaseSpeed = 2500, FuelConsumption = 1000,
            BaseDuration = TimeSpan.FromMinutes(5),
            Requirements = new() { { "Shipyard", 4 }, { "Impulse Drive", 3 } },
            IsColonizationCapable = true
        });
        
        ShipDefinitions.Add(new Ship
        {
            Id = "REC", Name = "Recycler", Description = "Harvests debris fields.",
            Image = "assets/ships/recycler.jpg",
            MetalCost = 10000, CrystalCost = 6000, DeuteriumCost = 2000,
            Structure = 16000, Shield = 10, Attack = 1, Capacity = 20000, BaseSpeed = 2000, FuelConsumption = 300,
            BaseDuration = TimeSpan.FromMinutes(3),
            Requirements = new() { { "Shipyard", 4 }, { "Combustion Drive", 6 }, { "Shielding Technology", 2 } },
            IsRecyclingCapable = true
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "ESP", Name = "Espionage Probe", Description = "Fast drone for spying.",
            Image = "assets/ships/probe.jpg",
            MetalCost = 0, CrystalCost = 1000, DeuteriumCost = 0,
            Structure = 1000, Shield = 0, Attack = 0, Capacity = 5, BaseSpeed = 100000000, FuelConsumption = 1,
            BaseDuration = TimeSpan.FromSeconds(5),
            Requirements = new() { { "Shipyard", 3 }, { "Combustion Drive", 3 }, { "Espionage Technology", 2 } },
            IsEspionageCapable = true
        });

        ShipDefinitions.Add(new Ship
        {
            Id = "DST", Name = "Destroyer", Description = "Anti-Deathstar specialized ship.",
             Image = "assets/ships/destroyer.jpg",
            MetalCost = 60000, CrystalCost = 50000, DeuteriumCost = 15000,
            Structure = 110000, Shield = 500, Attack = 2000, Capacity = 2000, BaseSpeed = 5000, FuelConsumption = 1000,
            BaseDuration = TimeSpan.FromMinutes(10),
            Requirements = new() { { "Shipyard", 9 }, { "Hyperspace Drive", 6 }, { "Hyperspace Technology", 5 } }
        });
        
        ShipDefinitions.Add(new Ship
        {
            Id = "RIP", Name = "Death Star", Description = "The ultimate weapon.",
            Image = "assets/ships/deathstar.jpg",
            MetalCost = 5000000, CrystalCost = 4000000, DeuteriumCost = 1000000,
            Structure = 9000000, Shield = 50000, Attack = 200000, Capacity = 1000000, BaseSpeed = 100, FuelConsumption = 1,
            BaseDuration = TimeSpan.FromHours(5),
            Requirements = new() { { "Shipyard", 12 }, { "Hyperspace Drive", 7 }, { "Graviton Technology", 1 } }
        });
    }

    private void AddDevModeShips()
    {
        foreach (var ship in ShipDefinitions)
        {
            if (DockedShips.ContainsKey(ship.Id))
            {
                DockedShips[ship.Id] += 10;
            }
            else
            {
                DockedShips[ship.Id] = 10;
            }
        }
        NotifyStateChanged();
    }

    public int GetShipCount(string shipId)
    {
        return DockedShips.ContainsKey(shipId) ? DockedShips[shipId] : 0;
    }

    // --- Fleet Operations ---

    public long CalculateFuelConsumption(Dictionary<string, int> shipsToSend, int targetGalaxy, int targetSystem, int targetPosition)
    {
        // Placeholder distance: 1 System = 1000 units, 1 Galaxy = 20000 units
        // Calculate distance from Home Planet
        long distance = Math.Abs(targetGalaxy - _galaxyService.HomeGalaxy) * 20000 + 
                        Math.Abs(targetSystem - _galaxyService.HomeSystem) * 1000 + 
                        Math.Abs(targetPosition - _galaxyService.HomePosition) * 5 + 1000;
        
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
        long distance = Math.Abs(targetGalaxy - _galaxyService.HomeGalaxy) * 20000 + 
                        Math.Abs(targetSystem - _galaxyService.HomeSystem) * 1000 + 
                        Math.Abs(targetPosition - _galaxyService.HomePosition) * 5 + 1000;
        
        // Time = Distance / Speed * Factor (e.g. 100)
        // With x100 speed universe, we divide result by 100
        double hours = (double)distance / minSpeed;
        double seconds = hours * 3600 / 100.0; // x100 Speed universe
        
        // Reduce attack time by 90%
        seconds = seconds * 0.1;
        
        if (seconds < 2) seconds = 2; // Minimum flight time

        return TimeSpan.FromSeconds(seconds);
    }

    public async Task<string?> SendFleet(Dictionary<string, int> shipsToSend, int g, int s, int p, string missionType)
    {
        if (!shipsToSend.Any()) return "No ships selected.";

        // Validate mission requirements
        var validationError = ValidateMission(missionType, g, s, p, shipsToSend);
        if (validationError != null) return validationError;

        // 1. Check Ship Availability
        foreach(var kvp in shipsToSend)
        {
            if (GetShipCount(kvp.Key) < kvp.Value) return $"Not enough {kvp.Key}.";
        }

        // 2. Calculate Fuel & Check Deuterium
        long fuel = CalculateFuelConsumption(shipsToSend, g, s, p);
        if (!await _resourceService.HasResourcesAsync(0, 0, fuel)) return "Not enough Deuterium for fuel.";

        // 3. Deduct Resources and Ships
        await _resourceService.ConsumeResourcesAsync(0, 0, fuel);
        foreach(var kvp in shipsToSend)
        {
            DockedShips[kvp.Key] -= kvp.Value;
        }
        await SaveToDatabaseAsync();

        // 4. Create Mission
        var flightTime = CalculateFlightTime(shipsToSend, g, s, p);
        flightTime = _devModeService.GetDuration(flightTime, 10); // Dev mode override

        var mission = new FleetMission
        {
            Id = Guid.NewGuid(),
            MissionType = missionType,
            OriginCoordinates = $"{_playerStateService.ActiveGalaxy}:{_playerStateService.ActiveSystem}:{_playerStateService.ActivePosition}",
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

    private string? ValidateMission(string missionType, int g, int s, int p, Dictionary<string, int> shipsToSend)
    {
        var planet = _galaxyService.GetPlanet(g, s, p);

        return missionType switch
        {
            "Attack" or "Transport" or "Espionage" or "Deploy" => planet?.IsOccupied == true 
                ? null 
                : $"Target [{g}:{s}:{p}] does not exist. Cannot perform {missionType.ToLower()} mission.",
            "Recycle" => planet?.HasDebris == true 
                ? null 
                : $"No debris field at [{g}:{s}:{p}]. Cannot perform recycle mission.",
            "Colonize" => ValidateColonize(g, s, p, planet, shipsToSend),
            "Expedition" => ValidateExpedition(shipsToSend),
            _ => null
        };
    }

    private string? ValidateColonize(int g, int s, int p, GalaxyPlanet? planet, Dictionary<string, int> shipsToSend)
    {
        // 1. Check if planet exists and is unoccupied
        if (planet == null)
            return $"Target [{g}:{s}:{p}] does not exist. Cannot colonize.";
        
        if (planet.IsOccupied)
            return $"Planet [{g}:{s}:{p}] is already occupied. Cannot colonize.";

        // 2. Check Astrophysics requirement
        int astroLevel = _technologyService.GetTechLevel(TechType.Astrophysics);
        if (astroLevel < 1)
            return "Colonization requires Astrophysics Technology Level 1. Research Astrophysics first.";

        // 3. Check planet limit (using global constant MaxPlanets = 4)
        int currentPlanetCount = _galaxyService.PlayerPlanets.Count;
        if (currentPlanetCount >= MaxPlanets)
            return $"You have reached the maximum number of planets ({currentPlanetCount}/{MaxPlanets}). You cannot colonize more planets.";

        // 4. Check for Colony Ship
        if (!shipsToSend.ContainsKey("CS") || shipsToSend["CS"] < 1)
            return "A Colony Ship (CS) is required to colonize a new planet.";

        // All validations passed
        return null;
    }

    private string? ValidateExpedition(Dictionary<string, int> shipsToSend)
    {
        // Expedition requires at least one ship
        if (!shipsToSend.Any())
            return "Expedition requires at least one ship.";

        // Check for Pathfinder (optional but recommended)
        // In OGame, Pathfinders give bonuses but aren't strictly required
        // We'll allow any ship for expedition
        return null;
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
                        if (mission.IsIncomingAttack)
                        {
                            HandleIncomingAttack(mission);
                            completedMissions.Add(mission);
                            NotifyStateChanged();
                            continue;
                        }

                        // Arrived!
                        await ProcessMissionArrival(mission);
                        
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
                        var originParts = mission.OriginCoordinates.Split(':');
                        int og = int.Parse(originParts[0]);
                        int os = int.Parse(originParts[1]);
                        int op = int.Parse(originParts[2]);

                        await SaveShipsToPlanetAsync(og, os, op, mission.Ships);

                        // Unload Cargo
                        if (mission.Cargo.Count > 0)
                        {
                            long m = mission.Cargo.ContainsKey("Metal") ? mission.Cargo["Metal"] : 0;
                            long c = mission.Cargo.ContainsKey("Crystal") ? mission.Cargo["Crystal"] : 0;
                            long d = mission.Cargo.ContainsKey("Deuterium") ? mission.Cargo["Deuterium"] : 0;
                            await _resourceService.AddResourcesToPlanetAsync(og, os, op, m, c, d);
                        }
                        
                        // We also need to reload active view if we are on that planet
                        if (_playerStateService.ActiveGalaxy == og && _playerStateService.ActiveSystem == os && _playerStateService.ActivePosition == op)
                        {
                            await LoadFromDatabaseAsync();
                        }
                        
                        _messageService.AddMessage("Fleet Return", 
                            $"Your fleet from {mission.TargetCoordinates} has returned to {mission.OriginCoordinates}.", 
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

    private async Task ProcessMissionArrival(FleetMission mission)
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
            case "Transport":
                await HandleTransport(mission, planet);
                break;
            case "Deploy":
                await HandleDeploy(mission, planet);
                break;
            case "Colonize":
                await HandleColonization(mission, planet);
                break;
            case "Recycle":
                HandleRecycle(mission, planet);
                break;
            case "Expedition":
                await HandleExpedition(mission);
                break;
            default:
                 _messageService.AddMessage("Fleet Reached Destination", 
                     $"Your fleet arrived at {mission.TargetCoordinates} and is returning.", "General");
                break;
        }
    }

    private async Task HandleExpedition(FleetMission mission)
    {
        var random = new Random();
        
        // Influence outcomes with Astrophysics (small bonus to finding things vs nothing)
        int astroLevel = _technologyService.GetTechLevel(TechType.Astrophysics);
        
        int roll = random.Next(1, 1000); 

        // Determine Fleet Capacity & Strength
        long totalCapacity = 0;
        long totalStructure = 0;
        foreach (var s in mission.Ships)
        {
            var def = ShipDefinitions.FirstOrDefault(x => x.Id == s.Key);
            if (def != null) 
            {
                totalCapacity += def.Capacity * s.Value;
                totalStructure += def.Structure * s.Value;
            }
        }
        
        // Calculate used cargo
        long currentLoad = mission.Cargo.Values.Sum();
        long availableCapacity = Math.Max(0, totalCapacity - currentLoad);

        if (roll <= 10) // 1% Black Hole
        {
            mission.Ships.Clear();
             _messageService.AddMessage("Expedition Result", 
                "The fleet encountered a singularity and was spaghettified. Total loss.", "Expedition");
        }
        else if (roll <= 100) // 9% Combat (Aliens/Pirates)
        {
            bool aliens = random.Next(0, 2) == 0;
            string enemyName = aliens ? "Aliens" : "Pirates";
            
            double damagePercent = aliens ? 0.3 : 0.1; // 30% or 10% loss
            
            var report = $"Your fleet was ambushed by {enemyName}!<br/>";
            
            foreach(var key in mission.Ships.Keys.ToList())
            {
                int count = mission.Ships[key];
                int loss = (int)Math.Ceiling(count * damagePercent);
                if (loss > 0)
                {
                    mission.Ships[key] -= loss;
                    if (mission.Ships[key] <= 0) mission.Ships.Remove(key);
                    var shipName = ShipDefinitions.FirstOrDefault(s => s.Id == key)?.Name ?? key;
                    report += $"Lost {loss} {shipName}(s)<br/>";
                }
            }
            
            if (mission.Ships.Count == 0) report += "The entire fleet was wiped out.";
            else report += "The rest of the fleet managed to escape.";

            _messageService.AddMessage("Expedition Combat", report, "Expedition");
        }
        else if (roll <= 400) // 30% Nothing or Delay
        {
            bool delay = random.Next(0, 2) == 0;
            if (delay)
            {
                 var delayTime = TimeSpan.FromMinutes(random.Next(30, 120)); // Minutes
                 mission.ReturnTime = mission.ReturnTime.Add(delayTime);
                 
                 _messageService.AddMessage("Expedition Result", 
                    $"The fleet got stuck in a nebula field. Return delayed by {delayTime.TotalMinutes:F0} minutes.", "Expedition");
            }
            else
            {
                 string[] flavorText = {
                    "The expedition found nothing but empty space.",
                    "Strange signals were detected, but they turned out to be background radiation.",
                    "The crew enjoyed a nice view of a supernova, but found nothing of value.",
                    "Main sensors malfunctioned for a while. Nothing to report."
                };
                _messageService.AddMessage("Expedition Result", flavorText[random.Next(flavorText.Length)], "Expedition");
            }
        }
        else if (roll <= 700) // 30% Resources
        {
            if (availableCapacity <= 0)
            {
                 _messageService.AddMessage("Expedition Result", 
                    "We found a rich asteroid belt, but our cargo holds are full!", "Expedition");
                 return;
            }

            // Size of find: Small, Medium, Large
            int sizeRoll = random.Next(1, 100);
            double multiplier = 1.0;
            string sizeDesc = "Small";
            
            if (sizeRoll > 90) { multiplier = 5.0; sizeDesc = "Huge"; }
            else if (sizeRoll > 50) { multiplier = 2.0; sizeDesc = "Medium"; }

            long baseAmount = (long)(totalStructure * 0.05 * multiplier); 
            baseAmount = Math.Clamp(baseAmount, 1000, 2000000);

            long m = 0, c = 0, d = 0;
            int typeRoll = random.Next(0, 3); // 0=Metal, 1=Crystal, 2=Deut (Mixed)
            
            if (typeRoll == 0) m = baseAmount;
            else if (typeRoll == 1) c = (long)(baseAmount * 0.5);
            else d = (long)(baseAmount * 0.33);

            // Cap by capacity
            if (m + c + d > availableCapacity)
            {
                double ratio = (double)availableCapacity / (m + c + d);
                m = (long)(m * ratio);
                c = (long)(c * ratio);
                d = (long)(d * ratio);
            }

            if (!mission.Cargo.ContainsKey("Metal")) mission.Cargo["Metal"] = 0;
            mission.Cargo["Metal"] += m;
            if (!mission.Cargo.ContainsKey("Crystal")) mission.Cargo["Crystal"] = 0;
            mission.Cargo["Crystal"] += c;
            if (!mission.Cargo.ContainsKey("Deuterium")) mission.Cargo["Deuterium"] = 0;
            mission.Cargo["Deuterium"] += d;

            _messageService.AddMessage("Expedition Result", 
                $"We found a {sizeDesc} resource deposit!<br/>Metal: {m:N0}<br/>Crystal: {c:N0}<br/>Deuterium: {d:N0}", "Expedition");
        }
        else if (roll <= 900) // 20% Ships
        {
            long pointsFound = (long)(totalStructure * random.NextDouble() * 0.1);
            if (pointsFound < 1000) pointsFound = 1000;

            var possibleShips = ShipDefinitions.Where(s => s.Id != "RIP" && s.Id != "DST" && s.Id != "CS").ToList();
            if (!possibleShips.Any()) return;

            string report = "We found abandoned ships drifting in space:<br/>";
            
            int safetyBreak = 0;
            while (pointsFound > 2000 && safetyBreak < 10)
            {
                var ship = possibleShips[random.Next(possibleShips.Count)];
                if (ship.Structure <= pointsFound)
                {
                    int count = random.Next(1, (int)(pointsFound / ship.Structure) + 1);
                    if (count > 10) count = 10;
                    
                    if (!mission.Ships.ContainsKey(ship.Id)) mission.Ships[ship.Id] = 0;
                    mission.Ships[ship.Id] += count;
                    
                    pointsFound -= count * ship.Structure;
                    report += $"{count} {ship.Name}<br/>";
                }
                safetyBreak++;
            }
            
            _messageService.AddMessage("Expedition Result", report, "Expedition");
        }
        else // 10% Dark Matter
        {
            int dm = random.Next(100, 1000) * (1 + astroLevel/5);
            await _resourceService.AddDarkMatterAsync(dm);
            
            _messageService.AddMessage("Expedition Result", 
                $"We found a pocket of Dark Matter! Obtained {dm} Dark Matter.", "Expedition");
        }
    }

    private void HandleEspionage(FleetMission mission, GalaxyPlanet? planet)
    {
        if (planet == null || !planet.IsOccupied)
        {
             _messageService.AddMessage("Espionage Report", 
                 $"Sector {mission.TargetCoordinates} is empty.", "Espionage");
             return;
        }

        // 1. Get Tech Levels
        int attackerSpyTech = _technologyService.GetTechLevel(TechType.Espionage);
        
        var (resources, defenses, ships, defenderSpyTech, buildings, techs) = GetPlanetActualState(planet);
        
        // 2. Counter-Espionage Calculation
        int probesCount = mission.Ships.ContainsKey("ESP") ? mission.Ships["ESP"] : 0;
        int defenderFleetCount = ships.Values.Sum(); // Total ships on planet
        
        double chance = Math.Min(100.0, (defenderFleetCount * 0.05) + (probesCount * 2.0));
        
        bool detected = new Random().NextDouble() * 100.0 < chance;
        
        string content = $"<strong>Target:</strong> {planet.Name} ({planet.PlayerName})<br/>" +
                         $"<strong>Counter-Intel Chance:</strong> {chance:F1}%<br/>";

        if (detected)
        {
            content += "<br/><span style='color:red; font-weight:bold;'>PROBES DETECTED AND DESTROYED!</span><br/>";
            content += "The defender's fleet engaged and eliminated your espionage unit.<br/>";
            
            // Destroy Probes
            mission.Ships.Clear(); // Fleet destroyed
        }

        // 3. Report Generation based on Level Difference
        int levelDiff = attackerSpyTech - defenderSpyTech;
        content += $"<strong>Tech Difference:</strong> {levelDiff} (Attacker: {attackerSpyTech} vs Defender: {defenderSpyTech})<br/><br/>";

        // Level >= 0: Resources (Base)
        if (levelDiff >= 0)
        {
             content += $"<strong>Resources:</strong><br/>" +
                       $"Metal: {resources["Metal"]:N0}<br/>" +
                       $"Crystal: {resources["Crystal"]:N0}<br/>" +
                       $"Deuterium: {resources["Deuterium"]:N0}<br/><br/>";
        }
        else
        {
            content += "<em>Signal too weak. Improve Espionage Technology to see resources.</em><br/><br/>";
        }

        // Level >= 1: Fleet
        if (levelDiff >= 1)
        {
            string fleetRows = "";
            foreach(var s in ships) if(s.Value > 0) fleetRows += $"{s.Key}: {s.Value:N0}<br/>";
            if (string.IsNullOrEmpty(fleetRows)) fleetRows = "No ships detected";
            content += $"<strong>Fleet:</strong><br/>{fleetRows}<br/><br/>";
        }

        // Level >= 2: Defense
        if (levelDiff >= 2)
        {
            string defRows = "";
            foreach(var d in defenses) if(d.Value > 0) defRows += $"{d.Key}: {d.Value:N0}<br/>";
            if (string.IsNullOrEmpty(defRows)) defRows = "No defense structures";
            content += $"<strong>Defense:</strong><br/>{defRows}<br/><br/>";
        }

        // Level >= 3: Buildings
        if (levelDiff >= 3)
        {
             string bRows = "";
             foreach(var b in buildings) if(b.Value > 0) bRows += $"{b.Key}: {b.Value}<br/>";
             if (string.IsNullOrEmpty(bRows)) bRows = "No buildings detected";
             content += $"<strong>Buildings:</strong><br/>{bRows}<br/><br/>";
        }

        // Level >= 4: Technology
        if (levelDiff >= 4)
        {
             string tRows = "";
             foreach(var t in techs) if(t.Value > 0) tRows += $"{t.Key}: {t.Value}<br/>";
             if (string.IsNullOrEmpty(tRows)) tRows = "No research detected";
             content += $"<strong>Research:</strong><br/>{tRows}<br/><br/>";
        }
        
        _messageService.AddMessage($"Spy Report [{mission.TargetCoordinates}]", content, "Espionage");
    }

    private void HandleCombat(FleetMission mission, GalaxyPlanet? planet)
    {
        if (planet == null || !planet.IsOccupied)
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

        foreach(var shipEntry in mission.Ships)
        {
             var def = ShipDefinitions.FirstOrDefault(x => x.Id == shipEntry.Key);
             if(def != null) 
             {
                 attackerAttack += def.Attack * shipEntry.Value;
                 attackerStructure += def.Structure * shipEntry.Value;
                 attackerShield += def.Shield * shipEntry.Value;
             }
        }
        
        // 2. Defender Power (Actual data from EnemyService)
        var (resources, defenses, ships, spyTech, buildings, techs) = GetPlanetActualState(planet);
        
        long defenderAttack = 0;
        long defenderStructure = 0;
        long defenderShield = 0;
        
        // Add Defenses
        foreach(var d in defenses)
        {
            var defUnit = _defenseService.DefenseDefinitions.FirstOrDefault(u => u.Name == d.Key);
            if (defUnit != null)
            {
                 defenderAttack += defUnit.Attack * d.Value;
                 defenderStructure += defUnit.Structure * d.Value;
                 defenderShield += defUnit.Shield * d.Value;
            }
        }
        
        // Add Ships to Defender
        foreach(var shipEntry in ships)
        {
             var def = ShipDefinitions.FirstOrDefault(x => x.Id == shipEntry.Key || x.Name == shipEntry.Key);
             if(def != null) 
             {
                 defenderAttack += def.Attack * shipEntry.Value;
                 defenderStructure += def.Structure * shipEntry.Value;
                 defenderShield += def.Shield * shipEntry.Value;
             }
        }

        // 3. Battle Resolution (Simplified)
        long attackerHealth = attackerStructure + attackerShield;
        long defenderHealth = defenderStructure + defenderShield;
        
        if(attackerHealth <= 0) attackerHealth = 1;
        if(defenderHealth <= 0) defenderHealth = 1;

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
            foreach(var shipEntry in mission.Ships) {
                 var def = ShipDefinitions.FirstOrDefault(x => x.Id == shipEntry.Key);
                 if(def!=null) totalCapacity += def.Capacity * shipEntry.Value;
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
                
                long unitMetal = 0;
                long unitCrystal = 0;

                if (defUnit != null)
                {
                    unitMetal = defUnit.MetalCost;
                    unitCrystal = defUnit.CrystalCost;
                }
                else
                {
                     if(d.Key.Contains("Rocket")) { unitMetal = 2000; }
                     else if(d.Key.Contains("Laser")) { unitMetal = 1500; unitCrystal = 500; }
                     else { unitMetal = 1000; }
                }

                debrisM += (long)(unitMetal * d.Value * 0.3);
                debrisC += (long)(unitCrystal * d.Value * 0.3);
            }
            
            // Also debris from destroyed Defender Ships (30%)
            foreach(var shipEntry in ships)
            {
                 var def = ShipDefinitions.FirstOrDefault(x => x.Name == shipEntry.Key);
                 if (def != null)
                 {
                     debrisM += (long)(def.MetalCost * shipEntry.Value * 0.3);
                     debrisC += (long)(def.CrystalCost * shipEntry.Value * 0.3);
                 }
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

        ApplyDefenseCombatLosses(defenses);
        
        _messageService.AddMessage($"Combat Report [{mission.TargetCoordinates}]", result, "Combat");
        
        // Notify enemy service about the attack
        var parts = mission.TargetCoordinates.Split(':');
        if (parts.Length == 3 && 
            int.TryParse(parts[0], out int g) && 
            int.TryParse(parts[1], out int s) && 
            int.TryParse(parts[2], out int p))
        {
            bool wasVictory = attackerScore > defenderScore;
            _ = _enemyService.OnPlayerAttack(g, s, p, wasVictory);
        }

    }

    // Helper to get real state for a planet
    private (Dictionary<string, long> Resources, Dictionary<string, int> Defenses, Dictionary<string, int> Ships, int SpyTech, Dictionary<string, int> Buildings, Dictionary<string, int> Techs) GetPlanetActualState(GalaxyPlanet planet)
    {
        var enemy = _enemyService.GetEnemy(planet.Galaxy, planet.System, planet.Position);
        
        if (enemy != null)
        {
            return (
                new Dictionary<string, long> { { "Metal", enemy.Metal }, { "Crystal", enemy.Crystal }, { "Deuterium", enemy.Deuterium } },
                enemy.Defenses,
                enemy.Ships,
                enemy.Technologies.GetValueOrDefault("Espionage Technology", 0),
                enemy.Buildings,
                enemy.Technologies
            );
        }

        // Fallback for non-enemy (or if not found), e.g. player planets handled elsewhere or empty
        return (new(), new(), new(), 0, new(), new());
    }

    private void ApplyDefenseCombatLosses(Dictionary<string, int> defenses)
    {
        double minLossPercentage = Math.Clamp(CombatDefenseLossMinPercentage, 0.0, 1.0);
        double maxLossPercentage = Math.Clamp(CombatDefenseLossMaxPercentage, 0.0, 1.0);
        if (maxLossPercentage < minLossPercentage)
        {
            (minLossPercentage, maxLossPercentage) = (maxLossPercentage, minLossPercentage);
        }
        if (maxLossPercentage <= 0 || defenses.Count == 0) return;

        double lossPercentage = minLossPercentage == maxLossPercentage
            ? minLossPercentage
            : minLossPercentage + (Random.Shared.NextDouble() * (maxLossPercentage - minLossPercentage));

        foreach (var defenseType in defenses.Keys.ToList())
        {
            int currentTotal = defenses[defenseType];
            if (currentTotal <= 0) continue;

            int lostAmount = (int)Math.Floor(currentTotal * lossPercentage);
            int updatedTotal = Math.Max(0, currentTotal - lostAmount);
            defenses[defenseType] = updatedTotal;
        }
    }

    private void HandleEnemyAttackLaunched(string originCoordinates, string targetCoordinates, Dictionary<string, int> ships)
    {
        var attackShips = ships.Where(s => s.Value > 0).ToDictionary(s => s.Key, s => s.Value);
        if (!attackShips.Any()) return;

        var targetParts = targetCoordinates.Split(':');
        if (targetParts.Length != 3 ||
            !int.TryParse(targetParts[0], out int tg) ||
            !int.TryParse(targetParts[1], out int ts) ||
            !int.TryParse(targetParts[2], out int tp))
        {
            return;
        }

        var targetPlanet = _galaxyService.GetPlanet(tg, ts, tp);
        if (targetPlanet == null || !targetPlanet.IsMyPlanet) return;

        if (!TryParseCoordinates(originCoordinates, out int og, out int os, out int op)) return;

        TimeSpan flightTime = CalculateFlightTimeFromCoordinates(attackShips, og, os, op, tg, ts, tp);
        if (flightTime <= TimeSpan.Zero) flightTime = TimeSpan.FromSeconds(30);

        var incomingMission = new FleetMission
        {
            Id = Guid.NewGuid(),
            MissionType = "Attack",
            OriginCoordinates = originCoordinates,
            TargetCoordinates = targetCoordinates,
            Ships = new Dictionary<string, int>(attackShips),
            StartTime = DateTime.Now,
            ArrivalTime = DateTime.Now.Add(flightTime),
            ReturnTime = DateTime.Now.Add(flightTime),
            Status = FleetStatus.Flight,
            IsIncomingAttack = true
        };

        ActiveFleets.Add(incomingMission);
        _messageService.AddMessage("Incoming Attack Warning", $"Enemy fleet detected from {originCoordinates} to {targetCoordinates}.", "Combat");
        NotifyStateChanged();
    }

    private TimeSpan CalculateFlightTimeFromCoordinates(Dictionary<string, int> shipsToSend, int originGalaxy, int originSystem, int originPosition, int targetGalaxy, int targetSystem, int targetPosition)
    {
        if (!shipsToSend.Any()) return TimeSpan.Zero;

        int minSpeed = int.MaxValue;
        foreach (var kvp in shipsToSend)
        {
            var ship = ShipDefinitions.FirstOrDefault(s => s.Id == kvp.Key);
            if (ship != null && ship.BaseSpeed < minSpeed) minSpeed = ship.BaseSpeed;
        }
        if (minSpeed == int.MaxValue) return TimeSpan.Zero;

        long distance = Math.Abs(targetGalaxy - originGalaxy) * 20000 +
                        Math.Abs(targetSystem - originSystem) * 1000 +
                        Math.Abs(targetPosition - originPosition) * 5 + 1000;

        double hours = (double)distance / minSpeed;
        double seconds = hours * 3600 / 100.0;
        seconds = seconds * 0.1;
        return TimeSpan.FromSeconds(seconds);
    }

    private static bool TryParseCoordinates(string coordinates, out int galaxy, out int system, out int position)
    {
        galaxy = 0;
        system = 0;
        position = 0;

        var parts = coordinates.Split(':');
        return parts.Length == 3 &&
               int.TryParse(parts[0], out galaxy) &&
               int.TryParse(parts[1], out system) &&
               int.TryParse(parts[2], out position);
    }

    private void HandleIncomingAttack(FleetMission mission)
    {
        _messageService.AddMessage("Planet Under Attack", $"Enemy attack reached {mission.TargetCoordinates}.", "Combat");
    }

    private async Task HandleTransport(FleetMission mission, GalaxyPlanet? planet)
    {
        if (planet == null || !planet.IsOccupied)
        {
            _messageService.AddMessage("Transport Failed", "Destination is empty.", "General");
            return;
        }

        long m = mission.Cargo.ContainsKey("Metal") ? mission.Cargo["Metal"] : 0;
        long c = mission.Cargo.ContainsKey("Crystal") ? mission.Cargo["Crystal"] : 0;
        long d = mission.Cargo.ContainsKey("Deuterium") ? mission.Cargo["Deuterium"] : 0;

        if (planet.IsMyPlanet)
        {
            await _resourceService.AddResourcesToPlanetAsync(planet.Galaxy, planet.System, planet.Position, m, c, d);
        }
        else
        {
            _messageService.AddMessage("Transport", "Resources delivered to target.", "General");
        }

        mission.Cargo.Clear();
        _messageService.AddMessage("Transport Successful", $"Resources delivered to {mission.TargetCoordinates}.", "General");
    }

    private async Task HandleDeploy(FleetMission mission, GalaxyPlanet? planet)
    {
        if (planet == null || !planet.IsMyPlanet)
        {
            _messageService.AddMessage("Deployment Failed", "You can only deploy to your own planets.", "General");
            return;
        }

        // Unload Cargo
        long m = mission.Cargo.ContainsKey("Metal") ? mission.Cargo["Metal"] : 0;
        long c = mission.Cargo.ContainsKey("Crystal") ? mission.Cargo["Crystal"] : 0;
        long d = mission.Cargo.ContainsKey("Deuterium") ? mission.Cargo["Deuterium"] : 0;
        await _resourceService.AddResourcesToPlanetAsync(planet.Galaxy, planet.System, planet.Position, m, c, d);
        mission.Cargo.Clear();

        // Transfer ships to the planet's docked ships
        await SaveShipsToPlanetAsync(planet.Galaxy, planet.System, planet.Position, mission.Ships);
        
        // If we are on that planet, reload
        if (_playerStateService.ActiveGalaxy == planet.Galaxy && _playerStateService.ActiveSystem == planet.System && _playerStateService.ActivePosition == planet.Position)
        {
            await LoadFromDatabaseAsync();
        }

        _messageService.AddMessage("Deployment successful", $"Fleet deployed to {mission.TargetCoordinates}.", "General");
        
        // Deployment doesn't return
        mission.Ships.Clear(); // They stay at target
        // Logic to add them to target planet's inventory needed here.
    }

    private async Task HandleColonization(FleetMission mission, GalaxyPlanet? planet)
    {
        if (planet == null || planet.IsOccupied)
        {
            _messageService.AddMessage("Colonization Failed", 
                 $"Planet {mission.TargetCoordinates} is already occupied!", "General");
             return;
        }

        // Check Astrophysics requirement
        int astroLevel = _technologyService.GetTechLevel(TechType.Astrophysics);
        if (astroLevel < 1)
        {
            _messageService.AddMessage("Colonization Failed", 
                 "Colonization requires Astrophysics Technology Level 1.", "General");
             return;
        }

        // Check Astrophysics Limit (using global constant MaxPlanets = 4)
        if (_galaxyService.PlayerPlanets.Count >= MaxPlanets)
        {
            _messageService.AddMessage("Colonization Failed", 
                 $"You have reached the maximum number of planets ({_galaxyService.PlayerPlanets.Count}/{MaxPlanets}). You cannot colonize more.", "General");
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
        planet.Image = "assets/planets/planet_colony.jpg";
        
        _galaxyService.RegisterPlanet(planet);

        // Initialize new planet state (Resources, Buildings, etc.)
        await _persistenceService.InitializePlanetAsync(planet.Galaxy, planet.System, planet.Position, true);
        await _persistenceService.AddOrUpdatePlayerPlanetAsync(planet.Galaxy, planet.System, planet.Position, planet.Name, planet.Image, false);

        _messageService.AddMessage("Colonization Successful", 
            $"You have successfully colonized position {mission.TargetCoordinates}!", "General");
    }

    private void HandleRecycle(FleetMission mission, GalaxyPlanet? planet)
    {
        if (planet == null || !planet.HasDebris)
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

    public TimeSpan CalculateShipConstructionDuration(Ship ship)
    {
        int shipyardLevel = _buildingService.GetBuildingLevel("Shipyard");
        int naniteLevel = _buildingService.GetBuildingLevel("Nanite Factory");
        
        long metalCost = ship.MetalCost;
        long crystalCost = ship.CrystalCost;
        
        // Formula: Time(hours) = (Metal + Crystal) / (5000 * (1 + Shipyard) * 2^Nanite * UniverseSpeed)
        double universeSpeed = 1.0;
        double divisor = 5000.0 * (1.0 + shipyardLevel) * Math.Pow(2, naniteLevel) * universeSpeed;
        
        double hours = (metalCost + crystalCost) / divisor;
        double seconds = hours * 3600.0;
        
        return TimeSpan.FromSeconds(seconds);
    }

    public async Task AddToQueueAsync(Ship ship, int quantity)
    {
        if (quantity <= 0) return;

        long totalMetal = ship.MetalCost * quantity;
        long totalCrystal = ship.CrystalCost * quantity;
        long totalDeuterium = ship.DeuteriumCost * quantity;

        if (await _resourceService.HasResourcesAsync(totalMetal, totalCrystal, totalDeuterium))
        {
            await _resourceService.ConsumeResourcesAsync(totalMetal, totalCrystal, totalDeuterium);
            
            var calculatedDuration = CalculateShipConstructionDuration(ship);
            var finalDuration = _devModeService.GetDuration(calculatedDuration, 1);

            ConstructionQueue.Add(new ShipyardItem
            {
                Ship = ship,
                Quantity = quantity,
                DurationPerUnit = finalDuration,
                TimeRemaining = finalDuration
            });
            
            // Notify enemy service that player is building ships
            _ = _enemyService.OnPlayerShipBuilt(ship.Id, quantity);
            
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
                    
                    await SaveToDatabaseAsync();
                    NotifyStateChanged();
                }
                NotifyStateChanged(); // Update timer UI
            }
            
            await Task.Delay(1000); // 1 second tick
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void ResetState()
    {
        DockedShips.Clear();
        ConstructionQueue.Clear();
        ActiveFleets.Clear();
        _isProcessingQueue = false;
        InitializeShips();
    }
}
