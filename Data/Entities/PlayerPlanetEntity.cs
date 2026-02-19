namespace myapp.Data.Entities;

public class PlayerPlanetEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public bool IsHomeworld { get; set; }
}
