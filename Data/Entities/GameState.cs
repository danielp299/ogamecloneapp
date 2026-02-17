namespace myapp.Data.Entities;

public class GameState
{
    public int Id { get; set; } = 1; // Single record
    
    // Resources
    public double Metal { get; set; } = 50000;
    public double Crystal { get; set; } = 50000;
    public double Deuterium { get; set; } = 50000;
    public double DarkMatter { get; set; } = 0;
    public long Energy { get; set; } = 0;
    public DateTime LastResourceUpdate { get; set; } = DateTime.UtcNow;
    
    // Game settings
    public bool DevModeEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;
    
    // Player home planet location (0,0,0 = not set, will generate random)
    public int HomeGalaxy { get; set; } = 0;
    public int HomeSystem { get; set; } = 0;
    public int HomePosition { get; set; } = 0;
}
