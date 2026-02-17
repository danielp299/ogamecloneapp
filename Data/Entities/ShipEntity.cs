namespace myapp.Data.Entities;

public class ShipEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ShipType { get; set; } = ""; // SC, LC, LF, HF, etc.
    public int Quantity { get; set; } = 0;
    
    // Coordinates
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }
}
