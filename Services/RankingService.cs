using Microsoft.EntityFrameworkCore;
using myapp.Data;

namespace myapp.Services;

public class RankingEntry
{
    public string PlayerKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsBot { get; set; }
    public double Points { get; set; }
    public int Stars { get; set; }   // victorias 0-3
    public int Defeats { get; set; } // derrotas (X marks) 0-3
}

public class RankingService
{
    private readonly GameDbContext _dbContext;
    private readonly Dictionary<string, RankingEntry> _rankings = new();
    private readonly SemaphoreSlim _dbSemaphore = new(1, 1);
    private bool _isInitialized = false;

    public const string PlayerKey = "player";
    public const string PlayerName = "Commander";

    public event Action? OnChange;

    public RankingService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        await EnsureTableExistsAsync();
        await LoadFromDatabaseAsync();
        EnsurePlayerEntry();
        _isInitialized = true;
    }

    private async Task EnsureTableExistsAsync()
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Rankings (
                PlayerKey TEXT NOT NULL PRIMARY KEY,
                DisplayName TEXT NOT NULL DEFAULT '',
                IsBot INTEGER NOT NULL DEFAULT 0,
                Points REAL NOT NULL DEFAULT 0,
                Stars INTEGER NOT NULL DEFAULT 0,
                Defeats INTEGER NOT NULL DEFAULT 0
            )";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task LoadFromDatabaseAsync()
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        // Remove stale entries that used planet coordinates as key (e.g. "1:2:3")
        await using (var cleanCmd = connection.CreateCommand())
        {
            cleanCmd.CommandText = "DELETE FROM Rankings WHERE PlayerKey GLOB '[0-9]*:[0-9]*:[0-9]*'";
            await cleanCmd.ExecuteNonQueryAsync();
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT PlayerKey, DisplayName, IsBot, Points, Stars, Defeats FROM Rankings";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var entry = new RankingEntry
            {
                PlayerKey = reader.GetString(0),
                DisplayName = reader.GetString(1),
                IsBot = reader.GetInt32(2) != 0,
                Points = reader.GetDouble(3),
                Stars = reader.GetInt32(4),
                Defeats = reader.GetInt32(5)
            };
            _rankings[entry.PlayerKey] = entry;
        }
    }

    private void EnsurePlayerEntry()
    {
        if (!_rankings.ContainsKey(PlayerKey))
        {
            _rankings[PlayerKey] = new RankingEntry
            {
                PlayerKey = PlayerKey,
                DisplayName = PlayerName,
                IsBot = false
            };
            _ = PersistEntryAsync(PlayerKey);
        }
    }

    // Pre-registers bots so they appear in the ranking even before any activity.
    // Pass one (empireId, homeworldName) per empire.
    public void EnsureBotEntries(IEnumerable<(string empireId, string name)> bots)
    {
        if (!_isInitialized) return;
        bool changed = false;
        foreach (var (empireId, name) in bots)
        {
            if (!_rankings.ContainsKey(empireId))
            {
                _rankings[empireId] = new RankingEntry { PlayerKey = empireId, DisplayName = name, IsBot = true };
                _ = PersistEntryAsync(empireId);
                changed = true;
            }
        }
        if (changed) OnChange?.Invoke();
    }

    private RankingEntry EnsureEntry(string playerKey, string displayName, bool isBot)
    {
        if (!_rankings.TryGetValue(playerKey, out var entry))
        {
            entry = new RankingEntry
            {
                PlayerKey = playerKey,
                DisplayName = displayName,
                IsBot = isBot
            };
            _rankings[playerKey] = entry;
        }
        return entry;
    }

    // Llamado cuando se gastan recursos en edificios, investigacion, naves o defensa
    public void AddSpendingPoints(string playerKey, string displayName, bool isBot, long metal, long crystal, long deuterium)
    {
        if (!_isInitialized) return;

        double points = (metal / 1000.0) + (crystal / 1000.0) * 1.2 + (deuterium / 1000.0) * 1.5;
        if (points <= 0) return;

        var entry = EnsureEntry(playerKey, displayName, isBot);
        entry.Points += points;

        _ = PersistEntryAsync(playerKey);
        OnChange?.Invoke();
    }

    // Llamado despues de resolver un combate.
    // defenderShipPts / defenderDefPts: valor en puntos de las unidades del defensor destruidas
    // attackerShipPts: valor en puntos de las naves del atacante destruidas
    // Atacante gana 10% de naves destruidas + 5% de defensas destruidas del defensor
    // Defensor gana 10% de naves del atacante destruidas
    // El dueno pierde el valor completo de sus unidades destruidas
    public void RecordCombat(
        string attackerKey, string attackerName, bool attackerIsBot,
        string defenderKey, string defenderName, bool defenderIsBot,
        bool attackerWon,
        double defenderShipPts,
        double defenderDefPts,
        double attackerShipPts)
    {
        if (!_isInitialized) return;

        var attacker = EnsureEntry(attackerKey, attackerName, attackerIsBot);
        var defender = EnsureEntry(defenderKey, defenderName, defenderIsBot);

        double attackerGain = defenderShipPts * 0.10 + defenderDefPts * 0.05;
        double defenderLoss = defenderShipPts + defenderDefPts;

        double defenderGain = attackerShipPts * 0.10;
        double attackerLoss = attackerShipPts;

        attacker.Points = Math.Max(0, attacker.Points + attackerGain - attackerLoss);
        defender.Points = Math.Max(0, defender.Points + defenderGain - defenderLoss);

        // Stars/Defeats only count in combats involving the player
        bool playerInvolved = attackerKey == PlayerKey || defenderKey == PlayerKey;
        if (playerInvolved)
        {
            if (attackerWon)
            {
                attacker.Stars = Math.Min(3, attacker.Stars + 1);
                defender.Defeats = Math.Min(3, defender.Defeats + 1);
            }
            else
            {
                defender.Stars = Math.Min(3, defender.Stars + 1);
                attacker.Defeats = Math.Min(3, attacker.Defeats + 1);
            }
        }

        _ = PersistEntryAsync(attackerKey);
        _ = PersistEntryAsync(defenderKey);
        OnChange?.Invoke();
    }

    public List<RankingEntry> GetRankings()
    {
        return _rankings.Values.OrderByDescending(r => r.Points).ToList();
    }

    public static double CalcPoints(long metal, long crystal, long deuterium)
        => (metal / 1000.0) + (crystal / 1000.0) * 1.2 + (deuterium / 1000.0) * 1.5;

    private async Task PersistEntryAsync(string playerKey)
    {
        if (!_rankings.TryGetValue(playerKey, out var entry)) return;

        await _dbSemaphore.WaitAsync();
        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Rankings (PlayerKey, DisplayName, IsBot, Points, Stars, Defeats)
                VALUES (@key, @name, @isBot, @points, @stars, @defeats)
                ON CONFLICT(PlayerKey) DO UPDATE SET
                    DisplayName = excluded.DisplayName,
                    Points = excluded.Points,
                    Stars = excluded.Stars,
                    Defeats = excluded.Defeats";

            AddParam(cmd, "@key", entry.PlayerKey);
            AddParam(cmd, "@name", entry.DisplayName);
            AddParam(cmd, "@isBot", entry.IsBot ? 1 : 0);
            AddParam(cmd, "@points", entry.Points);
            AddParam(cmd, "@stars", entry.Stars);
            AddParam(cmd, "@defeats", entry.Defeats);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RankingService] Error persisting {playerKey}: {ex.Message}");
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    public void ResetState()
    {
        _rankings.Clear();
        _isInitialized = false;
    }
}
