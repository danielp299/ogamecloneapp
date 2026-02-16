using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

public class ResourceService
{
    private readonly GameDbContext _dbContext;
    private readonly DevModeService _devModeService;
    private GameState? _cachedState;

    // Refund percentage for cancelled buildings/research (1-100)
    public double CancelRefundPercentage { get; set; } = 100.0;

    public event Action? OnChange;

    public ResourceService(GameDbContext dbContext, DevModeService devModeService)
    {
        _dbContext = dbContext;
        _devModeService = devModeService;
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

    public long Metal
    {
        get
        {
            var state = GetStateAsync().Result;
            var now = DateTime.UtcNow;
            var elapsedSeconds = (now - state.LastResourceUpdate).TotalSeconds;
            return (long)(state.Metal + (MetalProductionRate * elapsedSeconds));
        }
    }
    public long Crystal
    {
        get
        {
            var state = GetStateAsync().Result;
            var now = DateTime.UtcNow;
            var elapsedSeconds = (now - state.LastResourceUpdate).TotalSeconds;
            return (long)(state.Crystal + (CrystalProductionRate * elapsedSeconds));
        }
    }
    public long Deuterium
    {
        get
        {
            var state = GetStateAsync().Result;
            var now = DateTime.UtcNow;
            var elapsedSeconds = (now - state.LastResourceUpdate).TotalSeconds;
            return (long)(state.Deuterium + (DeuteriumProductionRate * elapsedSeconds));
        }
    }
    public long DarkMatter => (long)(GetStateAsync().Result.DarkMatter);
    public long Energy => GetStateAsync().Result.Energy;

    public double MetalProductionRate { get; set; }
    public double CrystalProductionRate { get; set; }
    public double DeuteriumProductionRate { get; set; }

private async Task SaveStateAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    private void EnsureCurrentResourcesCalculated(GameState state)
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

    public bool HasResources(long metal, long crystal, long deuterium)
    {
        var state = GetStateAsync().Result;
        EnsureCurrentResourcesCalculated(state);
        return state.Metal >= metal && state.Crystal >= crystal && state.Deuterium >= deuterium;
    }

    public bool ConsumeResources(long metal, long crystal, long deuterium)
    {
        if (!HasResources(metal, crystal, deuterium))
        {
            return false;
        }

        var state = GetStateAsync().Result;
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
        var state = GetStateAsync().Result;
        EnsureCurrentResourcesCalculated(state);
        state.Metal += metal;
        state.Crystal += crystal;
        state.Deuterium += deuterium;

        _dbContext.SaveChanges();
        NotifyStateChanged();
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
        var state = GetStateAsync().Result;
        state.Energy = energy;

        _dbContext.SaveChanges();
        NotifyStateChanged();
    }

    public void UpdateEnergy(long deltaEnergy)
    {
        var state = GetStateAsync().Result;
        state.Energy += deltaEnergy;

        _dbContext.SaveChanges();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
