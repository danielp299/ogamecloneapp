namespace myapp.Data.Entities;

public class ShipEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ShipType { get; set; } = ""; // SC, LC, LF, HF, etc.
    public int Quantity { get; set; } = 0;
}
