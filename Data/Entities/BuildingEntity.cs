namespace myapp.Data.Entities;

public class BuildingEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BuildingType { get; set; } = ""; // Metal Mine, Crystal Mine, etc.
    public int Level { get; set; } = 0;
}
