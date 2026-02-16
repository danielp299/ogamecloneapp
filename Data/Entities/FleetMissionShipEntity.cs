namespace myapp.Data.Entities;

public class FleetMissionShipEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FleetMissionId { get; set; }
    public string ShipType { get; set; } = ""; // SC, LC, LF, etc.
    public int Quantity { get; set; } = 0;
    
    public FleetMissionEntity FleetMission { get; set; } = null!;
}
