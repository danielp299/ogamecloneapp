namespace myapp.Data.Entities;

public class DefenseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DefenseType { get; set; } = ""; // RL, LL, HL, etc.
    public int Quantity { get; set; } = 0;
    
    // Coordinates
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }
}
