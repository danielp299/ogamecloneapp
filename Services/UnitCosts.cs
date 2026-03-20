namespace myapp.Services;

/// <summary>
/// Single source of truth for unit construction costs shared by EnemyService
/// and any other service that needs cost data without introducing circular
/// dependencies with FleetService / DefenseService.
/// </summary>
public static class UnitCosts
{
    public static (long Metal, long Crystal, long Deuterium) Ship(string shipId) => shipId switch
    {
        "SC"  => (2_000,       2_000,        0),
        "LC"  => (6_000,       6_000,        0),
        "LF"  => (3_000,       1_000,        0),
        "HF"  => (6_000,       4_000,        0),
        "CR"  => (20_000,      7_000,    2_000),
        "BS"  => (45_000,     15_000,        0),
        "CS"  => (10_000,     20_000,   10_000),
        "REC" => (10_000,      6_000,    2_000),
        "ESP" => (0,           1_000,        0),
        "DST" => (60_000,     50_000,   15_000),
        "RIP" => (5_000_000, 4_000_000, 1_000_000),
        _     => (5_000,       2_000,        0)
    };

    public static (long Metal, long Crystal, long Deuterium) Defense(string defenseName) => defenseName switch
    {
        "Rocket Launcher"       => (2_000,  0,      0),
        "Light Laser"           => (1_500,  500,    0),
        "Heavy Laser"           => (6_000,  2_000,  0),
        "Gauss Cannon"          => (20_000, 15_000, 2_000),
        "Ion Cannon"            => (2_000,  6_000,  0),
        "Plasma Turret"         => (50_000, 50_000, 30_000),
        "Small Shield Dome"     => (10_000, 10_000, 0),
        "Large Shield Dome"     => (50_000, 50_000, 0),
        "Anti-Ballistic Missile"=> (8_000,  0,      2_000),
        _                       => (1_000,  500,    0)
    };

    public static (long Metal, long Crystal, long Deuterium) Building(string buildingName, int currentLevel)
    {
        const double Scaling = 2.0;
        long bm = buildingName switch
        {
            "Metal Mine"            => 60,
            "Crystal Mine"          => 48,
            "Deuterium Synthesizer" => 225,
            "Solar Plant"           => 75,
            "Robotics Factory"      => 400,
            "Shipyard"              => 400,
            "Research Lab"          => 200,
            "Metal Storage"         => 1_000,
            "Crystal Storage"       => 1_000,
            "Deuterium Tank"        => 1_000,
            "Fusion Reactor"        => 900,
            "Nanite Factory"        => 1_000_000,
            _                       => 500
        };
        long bc = buildingName switch
        {
            "Metal Mine"            => 15,
            "Crystal Mine"          => 24,
            "Deuterium Synthesizer" => 75,
            "Solar Plant"           => 30,
            "Robotics Factory"      => 120,
            "Shipyard"              => 200,
            "Research Lab"          => 400,
            "Crystal Storage"       => 500,
            "Deuterium Tank"        => 1_000,
            "Fusion Reactor"        => 360,
            "Nanite Factory"        => 1_000_000,
            _                       => 250
        };
        long bd = buildingName switch
        {
            "Robotics Factory" => 200,
            "Shipyard"         => 100,
            "Research Lab"     => 200,
            "Fusion Reactor"   => 180,
            _                  => 0
        };
        double factor = Math.Pow(Scaling, currentLevel);
        return ((long)(bm * factor), (long)(bc * factor), (long)(bd * factor));
    }

    public static (long Metal, long Crystal, long Deuterium) Technology(string techName, int currentLevel)
    {
        const double Scaling = 2.0;
        long bm = techName switch
        {
            "Espionage Technology"  => 200,
            "Weapons Technology"    => 800,
            "Shielding Technology"  => 200,
            "Armour Technology"     => 1_000,
            "Combustion Drive"      => 400,
            "Impulse Drive"         => 2_000,
            "Hyperspace Drive"      => 10_000,
            "Laser Technology"      => 200,
            "Ion Technology"        => 1_000,
            "Plasma Technology"     => 2_000,
            _                       => 500
        };
        long bc = techName switch
        {
            "Espionage Technology"  => 1_000,
            "Computer Technology"   => 400,
            "Weapons Technology"    => 200,
            "Shielding Technology"  => 600,
            "Energy Technology"     => 800,
            "Hyperspace Technology" => 4_000,
            "Impulse Drive"         => 4_000,
            "Hyperspace Drive"      => 20_000,
            "Laser Technology"      => 100,
            "Ion Technology"        => 300,
            "Plasma Technology"     => 4_000,
            _                       => 250
        };
        long bd = techName switch
        {
            "Espionage Technology"  => 200,
            "Computer Technology"   => 600,
            "Energy Technology"     => 400,
            "Hyperspace Technology" => 2_000,
            "Combustion Drive"      => 600,
            "Impulse Drive"         => 600,
            "Hyperspace Drive"      => 6_000,
            "Ion Technology"        => 100,
            "Plasma Technology"     => 1_000,
            _                       => 0
        };
        double factor = Math.Pow(Scaling, currentLevel);
        return ((long)(bm * factor), (long)(bc * factor), (long)(bd * factor));
    }
}
