namespace myapp.Data.Entities;

public class ResearchQueueEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TechnologyType { get; set; } = "";
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; }
    public bool IsProcessing { get; set; } = false;
}
