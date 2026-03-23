using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

/// <summary>
/// Manages the player profile (single row in DB).
/// Creates the profile automatically if it doesn't exist.
/// Syncs CurrentSkin with the static SkinConfig.
/// </summary>
public class PlayerProfileService
{
    private readonly GameDbContext _db;

    public string PlayerName { get; private set; } = "Player";
    public string CurrentSkin { get; private set; } = "skin1";

    public event Action? OnChange;

    public PlayerProfileService(GameDbContext db)
    {
        _db = db;
        Load();
    }

    private void Load()
    {
        EnsurePlayerProfileTable();

        var profile = _db.PlayerProfiles.FirstOrDefault();
        if (profile == null)
        {
            profile = new PlayerProfileEntity { Id = 1, PlayerName = "Player", CurrentSkin = "skin1" };
            _db.PlayerProfiles.Add(profile);
            _db.SaveChanges();
        }

        PlayerName = profile.PlayerName;
        CurrentSkin = profile.CurrentSkin;

        // Sync static helper used by Razor pages
        SkinConfig.CurrentSkin = CurrentSkin;
    }

    private void EnsurePlayerProfileTable()
    {
        _db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "PlayerProfiles" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PlayerProfiles" PRIMARY KEY,
                "PlayerName" TEXT NOT NULL,
                "CurrentSkin" TEXT NOT NULL
            );
            """);
    }

    public async Task SetSkinAsync(string skin)
    {
        CurrentSkin = skin;
        SkinConfig.CurrentSkin = skin;

        var profile = await _db.PlayerProfiles.FirstOrDefaultAsync()
                      ?? new PlayerProfileEntity { Id = 1 };
        profile.CurrentSkin = skin;

        _db.PlayerProfiles.Update(profile);
        await _db.SaveChangesAsync();

        OnChange?.Invoke();
    }

    public async Task SetPlayerNameAsync(string name)
    {
        PlayerName = name;

        var profile = await _db.PlayerProfiles.FirstOrDefaultAsync()
                      ?? new PlayerProfileEntity { Id = 1 };
        profile.PlayerName = name;

        _db.PlayerProfiles.Update(profile);
        await _db.SaveChangesAsync();

        OnChange?.Invoke();
    }
}
