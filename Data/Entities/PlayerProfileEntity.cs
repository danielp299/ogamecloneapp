namespace myapp.Data.Entities;

/// <summary>
/// Single-row entity (Id = 1) that stores player-wide preferences.
/// </summary>
public class PlayerProfileEntity
{
    public int Id { get; set; } = 1;
    public string PlayerName { get; set; } = "Player";
    public string CurrentSkin { get; set; } = "skin1";
}
