namespace myapp.Data.Entities;

public class BuildingEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BuildingType { get; set; } = ""; // Metal Mine, Crystal Mine, etc.
    public int Level { get; set; } = 0;
    
    // Coordinates for the planet where the building is located
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }
}
