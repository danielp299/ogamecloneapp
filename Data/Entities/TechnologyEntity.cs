namespace myapp.Data.Entities;

public class TechnologyEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TechnologyType { get; set; } = ""; // Espionage, Computer, etc.
    public int Level { get; set; } = 0;
}
