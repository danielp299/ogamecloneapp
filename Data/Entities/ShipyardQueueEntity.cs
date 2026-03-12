namespace myapp.Data.Entities;

public class ShipyardQueueEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ShipType { get; set; } = "";
    public int Quantity { get; set; } = 0;
    public int QuantityCompleted { get; set; } = 0;
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; }
    public bool IsProcessing { get; set; } = false;

    public TimeSpan Duration => EndTime - StartTime;
    public bool IsCompleted => DateTime.UtcNow >= EndTime;
    public TimeSpan TimeRemaining => IsCompleted ? TimeSpan.Zero : EndTime - DateTime.UtcNow;
}
