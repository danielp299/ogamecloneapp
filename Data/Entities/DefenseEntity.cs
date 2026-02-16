namespace myapp.Data.Entities;

public class DefenseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DefenseType { get; set; } = ""; // RL, LL, HL, etc.
    public int Quantity { get; set; } = 0;
}
