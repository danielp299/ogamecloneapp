using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

// Refer to wiki/business-rules/RESOURCE_LOGIC_SUMMARY.md for business rules documentation

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
                throw new InvalidOperationException($"Planet state for {key} not found");
            }
            _cachedPlanetStates[key] = state;
        }
        return state;
    }

    private async Task<PlanetState> GetActivePlanetStateAsync()
    {
        return await GetPlanetStateAsync(_playerStateService.ActiveGalaxy, _playerStateService.ActiveSystem, _playerStateService.ActivePosition);
    }

    public async Task<ResourceSnapshot> GetResourceSnapshotAsync()
    {
        var state = await GetStateAsync();
        var planetState = await GetActivePlanetStateAsync();
        EnsureCurrentResourcesCalculated(planetState);

        return new ResourceSnapshot(
            (long)planetState.Metal,
            (long)planetState.Crystal,
            (long)planetState.Deuterium,
            planetState.Energy,
            (long)state.DarkMatter
        );
    }

    public async Task<long> GetEnergyAsync()
    {
        var state = await GetActivePlanetStateAsync();
        return state.Energy;
    }

    public double MetalProductionRate { get; set; }
    public double CrystalProductionRate { get; set; }
    public double DeuteriumProductionRate { get; set; }

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
            state.Metal += MetalProductionRate * elapsedSeconds;
            state.Crystal += CrystalProductionRate * elapsedSeconds;
            state.Deuterium += DeuteriumProductionRate * elapsedSeconds;
            state.LastResourceUpdate = now;
        }
    }

    public async Task<bool> HasResourcesAsync(long metal, long crystal, long deuterium)
    {
        var state = await GetActivePlanetStateAsync();
        EnsureCurrentResourcesCalculated(state);
        return state.Metal >= metal && state.Crystal >= crystal && state.Deuterium >= deuterium;
    }

    public async Task<bool> ConsumeResourcesAsync(long metal, long crystal, long deuterium)
    {
        if (!await HasResourcesAsync(metal, crystal, deuterium))
        {
            return false;
        }

        var state = await GetActivePlanetStateAsync();
        EnsureCurrentResourcesCalculated(state);
        state.Metal -= metal;
        state.Crystal -= crystal;
        state.Deuterium -= deuterium;

        await _dbContext.SaveChangesAsync();
        NotifyStateChanged();
        return true;
    }

    public async Task AddResourcesAsync(double metal, double crystal, double deuterium)
    {
        var state = await GetActivePlanetStateAsync();
        EnsureCurrentResourcesCalculated(state);
        state.Metal += metal;
        state.Crystal += crystal;
        state.Deuterium += deuterium;

        await _dbContext.SaveChangesAsync();
        NotifyStateChanged();
    }

    public async Task AddResourcesToPlanetAsync(int g, int s, int p, double metal, double crystal, double deuterium)
    {
        var state = await GetPlanetStateAsync(g, s, p);
        EnsureCurrentResourcesCalculated(state);
        state.Metal += metal;
        state.Crystal += crystal;
        state.Deuterium += deuterium;

        await _dbContext.SaveChangesAsync();
        if (_playerStateService.ActiveGalaxy == g && _playerStateService.ActiveSystem == s && _playerStateService.ActivePosition == p)
        {
            NotifyStateChanged();
        }
    }

    public async Task AddDarkMatterAsync(long amount)
    {
        var state = await GetStateAsync();
        state.DarkMatter += amount;

        await _dbContext.SaveChangesAsync();
        NotifyStateChanged();
    }

    public async Task RefundResourcesAsync(long metal, long crystal, long deuterium)
    {
        var percentage = Math.Clamp(CancelRefundPercentage, 0.0, 100.0) / 100.0;
        await AddResourcesAsync(metal * percentage, crystal * percentage, deuterium * percentage);
    }

    public async Task SetEnergyAsync(long energy)
    {
        var state = await GetActivePlanetStateAsync();
        state.Energy = energy;

        await _dbContext.SaveChangesAsync();
        NotifyStateChanged();
    }

    public async Task UpdateEnergyAsync(long deltaEnergy)
    {
        var state = await GetActivePlanetStateAsync();
        state.Energy += deltaEnergy;

        await _dbContext.SaveChangesAsync();
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

public record ResourceSnapshot(long Metal, long Crystal, long Deuterium, long Energy, long DarkMatter);
