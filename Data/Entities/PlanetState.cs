namespace myapp.Data.Entities;

public class PlanetState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Coordinates
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }
    
    // Resources
    public double Metal { get; set; } = 500;
    public double Crystal { get; set; } = 500;
    public double Deuterium { get; set; } = 0;
    public long Energy { get; set; } = 0;
    public DateTime LastResourceUpdate { get; set; } = DateTime.UtcNow;
}
