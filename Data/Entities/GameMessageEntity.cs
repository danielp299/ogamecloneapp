namespace myapp.Data.Entities;

public class GameMessageEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string Type { get; set; } = "General"; // Combat, Espionage, Expedition, General
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
