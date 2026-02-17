using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

public class ResourceService
{
    private readonly GameDbContext _dbContext;
    private readonly DevModeService _devModeService;
    private readonly PlayerStateService _playerStateService;
    private GameState? _cachedState;
    private Dictionary<string, PlanetState> _cachedPlanetStates = new();

    // Refund percentage for cancelled buildings/research (1-100)
    public double CancelRefundPercentage { get; set; } = 100.0;

    public event Action? OnChange;

    public ResourceService(GameDbContext dbContext, DevModeService devModeService, PlayerStateService playerStateService)
    {
        _dbContext = dbContext;
        _devModeService = devModeService;
        _playerStateService = playerStateService;
        
        _playerStateService.OnChange += () => NotifyStateChanged();
    }

    public async Task LoadStateAsync()
    {
        _cachedState = await _dbContext.GameState.FirstOrDefaultAsync();
    }

    private async Task<GameState> GetStateAsync()
    {
        if (_cachedState == null)
        {
            _cachedState = await _dbContext.GameState.FirstOrDefaultAsync();
        }
        return _cachedState ?? throw new InvalidOperationException("Game state not initialized");
    }

    private async Task<PlanetState> GetPlanetStateAsync(int g, int s, int p)
    {
        string key = $"{g}:{s}:{p}";
        if (!_cachedPlanetStates.TryGetValue(key, out var state))
        {
            state = await _dbContext.PlanetStates.FirstOrDefaultAsync(ps => ps.Galaxy == g && ps.System == s && ps.Position == p);
            if (state == null)
            {
                // If it doesn't exist, we might be in a race condition during colonization, 
                // but usually we should have it.
                throw new InvalidOperationException($"Planet state for {key} not found");
            }
            _cachedPlanetStates[key] = state;
        }
        return state;
    }

    private PlanetState GetActivePlanetState()
    {
        return GetPlanetStateAsync(_playerStateService.ActiveGalaxy, _playerStateService.ActiveSystem, _playerStateService.ActivePosition).Result;
    }

    public long Metal => (long)GetActivePlanetState().Metal;
    public long Crystal => (long)GetActivePlanetState().Crystal;
    public long Deuterium => (long)GetActivePlanetState().Deuterium;
    public long DarkMatter => (long)(GetStateAsync().Result.DarkMatter);
    public long Energy => GetActivePlanetState().Energy;

    public double MetalProductionRate { get; set; }
    public double CrystalProductionRate { get; set; }
    public double DeuteriumProductionRate { get; set; }

    // ... updated methods follow ...

private async Task SaveStateAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    private void EnsureCurrentResourcesCalculated(PlanetState state)
    {
        var now = DateTime.UtcNow;
        var elapsedSeconds = (now - state.LastResourceUpdate).TotalSeconds;

        if (elapsedSeconds > 0)
        {
            // Note: This still uses the SHARED MetalProductionRate. 
            // We'll need to fix this in BuildingService soon.
            state.Metal += MetalProductionRate * elapsedSeconds;
            state.Crystal += CrystalProductionRate * elapsedSeconds;
            state.Deuterium += DeuteriumProductionRate * elapsedSeconds;
            state.LastResourceUpdate = now;
        }
    }

    public bool HasResources(long metal, long crystal, long deuterium)
    {
        var state = GetActivePlanetState();
        EnsureCurrentResourcesCalculated(state);
        return state.Metal >= metal && state.Crystal >= crystal && state.Deuterium >= deuterium;
    }

    public bool ConsumeResources(long metal, long crystal, long deuterium)
    {
        if (!HasResources(metal, crystal, deuterium))
        {
            return false;
        }

        var state = GetActivePlanetState();
        EnsureCurrentResourcesCalculated(state);
        state.Metal -= metal;
        state.Crystal -= crystal;
        state.Deuterium -= deuterium;

        _dbContext.SaveChanges();
        NotifyStateChanged();
        return true;
    }

    public void AddResources(double metal, double crystal, double deuterium)
    {
        var state = GetActivePlanetState();
        EnsureCurrentResourcesCalculated(state);
        state.Metal += metal;
        state.Crystal += crystal;
        state.Deuterium += deuterium;

        _dbContext.SaveChanges();
        NotifyStateChanged();
    }

    public void AddResourcesToPlanet(int g, int s, int p, double metal, double crystal, double deuterium)
    {
        var state = GetPlanetStateAsync(g, s, p).Result;
        EnsureCurrentResourcesCalculated(state);
        state.Metal += metal;
        state.Crystal += crystal;
        state.Deuterium += deuterium;

        _dbContext.SaveChanges();
        // We only notify if it's the active planet
        if (_playerStateService.ActiveGalaxy == g && _playerStateService.ActiveSystem == s && _playerStateService.ActivePosition == p)
        {
            NotifyStateChanged();
        }
    }

    public void AddDarkMatter(long amount)
    {
        var state = GetStateAsync().Result;
        state.DarkMatter += amount;

        _dbContext.SaveChanges();
        NotifyStateChanged();
    }

    public void RefundResources(long metal, long crystal, long deuterium)
    {
        var percentage = Math.Clamp(CancelRefundPercentage, 0.0, 100.0) / 100.0;
        AddResources(metal * percentage, crystal * percentage, deuterium * percentage);
    }

    public void SetEnergy(long energy)
    {
        var state = GetActivePlanetState();
        state.Energy = energy;

        _dbContext.SaveChanges();
        NotifyStateChanged();
    }

    public void UpdateEnergy(long deltaEnergy)
    {
        var state = GetActivePlanetState();
        state.Energy += deltaEnergy;

        _dbContext.SaveChanges();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void ResetState()
    {
        _cachedState = null;
        _cachedPlanetStates.Clear();
        CancelRefundPercentage = 100.0;
    }
}
