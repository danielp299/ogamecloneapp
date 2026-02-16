namespace myapp.Data.Entities;

public class DefenseQueueEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DefenseType { get; set; } = "";
    public int Quantity { get; set; } = 0;
    public int QuantityCompleted { get; set; } = 0;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; }
    public bool IsProcessing { get; set; } = false;
}
