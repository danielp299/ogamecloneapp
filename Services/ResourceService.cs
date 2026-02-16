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

    public long Metal => (long)(GetStateAsync().Result.Metal);
    public long Crystal => (long)(GetStateAsync().Result.Crystal);
    public long Deuterium => (long)(GetStateAsync().Result.Deuterium);
    public long DarkMatter => (long)(GetStateAsync().Result.DarkMatter);
    public long Energy => GetStateAsync().Result.Energy;

    // Synchronous version for backward compatibility during migration
    public void UpdateResources(double metalRate, double crystalRate, double deuteriumRate)
    {
        var state = GetStateAsync().Result;
        var now = DateTime.UtcNow;
        var elapsedSeconds = (now - state.LastResourceUpdate).TotalSeconds;

        if (elapsedSeconds > 0)
        {
            state.Metal += metalRate * elapsedSeconds;
            state.Crystal += crystalRate * elapsedSeconds;
            state.Deuterium += deuteriumRate * elapsedSeconds;
            state.LastResourceUpdate = now;

            _dbContext.SaveChanges();
            NotifyStateChanged();
        }
    }

    public async Task UpdateResourcesAsync(double metalRate, double crystalRate, double deuteriumRate)
    {
        var state = await GetStateAsync();
        var now = DateTime.UtcNow;
        var elapsedSeconds = (now - state.LastResourceUpdate).TotalSeconds;

        if (elapsedSeconds > 0)
        {
            state.Metal += metalRate * elapsedSeconds;
            state.Crystal += crystalRate * elapsedSeconds;
            state.Deuterium += deuteriumRate * elapsedSeconds;
            state.LastResourceUpdate = now;

            await _dbContext.SaveChangesAsync();
            NotifyStateChanged();
        }
    }

    public bool HasResources(long metal, long crystal, long deuterium)
    {
        var state = GetStateAsync().Result;
        return state.Metal >= metal && state.Crystal >= crystal && state.Deuterium >= deuterium;
    }

    public bool ConsumeResources(long metal, long crystal, long deuterium)
    {
        if (!HasResources(metal, crystal, deuterium))
        {
            return false;
        }

        var state = GetStateAsync().Result;
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
