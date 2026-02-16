namespace myapp.Data.Entities;

public class FleetMissionEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MissionType { get; set; } = ""; // Attack, Transport, Espionage, etc.
    public int TargetGalaxy { get; set; }
    public int TargetSystem { get; set; }
    public int TargetPosition { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime ArrivalTime { get; set; }
    public DateTime ReturnTime { get; set; }
    public string Status { get; set; } = "Flight"; // Flight, Return, Holding
    public long FuelConsumed { get; set; } = 0;
    public long CargoMetal { get; set; } = 0;
    public long CargoCrystal { get; set; } = 0;
    public long CargoDeuterium { get; set; } = 0;
}
