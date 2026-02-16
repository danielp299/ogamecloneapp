namespace myapp.Data.Entities;

public class BuildingQueueEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BuildingType { get; set; } = "";
    public int TargetLevel { get; set; } = 1;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; }
    public bool IsProcessing { get; set; } = false;
    public int QueuePosition { get; set; } = 0;
}
