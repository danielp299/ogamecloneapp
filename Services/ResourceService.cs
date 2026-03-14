using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

// Refer to wiki/business-rules/RESOURCE_LOGIC_SUMMARY.md for business rules documentation

public class ResourceService
{
    private const string MetalStorageBuilding = "Metal Storage";
    private const string CrystalStorageBuilding = "Crystal Storage";
    private const string DeuteriumStorageBuilding = "Deuterium Tank";
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
        var storageCapacities = await GetActivePlanetStorageCapacitiesAsync();
        return BuildPreviewSnapshot(state, planetState, storageCapacities, DateTime.UtcNow);
    }

    public async Task<ResourceDisplayState> GetResourceDisplayStateAsync()
    {
        var state = await GetStateAsync();
        var planetState = await GetActivePlanetStateAsync();
        var storageCapacities = await GetActivePlanetStorageCapacitiesAsync();
        var snapshot = BuildPreviewSnapshot(state, planetState, storageCapacities, DateTime.UtcNow);
        return new ResourceDisplayState(snapshot, storageCapacities);
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

    private ResourceSnapshot BuildPreviewSnapshot(GameState gameState, PlanetState planetState, ResourceStorageCapacities storageCapacities, DateTime now)
    {
        var elapsedSeconds = (now - planetState.LastResourceUpdate).TotalSeconds;
        if (elapsedSeconds < 0) elapsedSeconds = 0;

        var metal = ResourceStorageRules.ApplyProductionLimit(planetState.Metal, MetalProductionRate, elapsedSeconds, storageCapacities.Metal);
        var crystal = ResourceStorageRules.ApplyProductionLimit(planetState.Crystal, CrystalProductionRate, elapsedSeconds, storageCapacities.Crystal);
        var deuterium = ResourceStorageRules.ApplyProductionLimit(planetState.Deuterium, DeuteriumProductionRate, elapsedSeconds, storageCapacities.Deuterium);

        return new ResourceSnapshot(
            (long)metal,
            (long)crystal,
            (long)deuterium,
            planetState.Energy,
            (long)gameState.DarkMatter
        );
    }

    private void SettlePlanetResourcesToNow(PlanetState planetState, ResourceStorageCapacities storageCapacities, DateTime now)
    {
        var elapsedSeconds = (now - planetState.LastResourceUpdate).TotalSeconds;
        if (elapsedSeconds < 0) elapsedSeconds = 0;

        planetState.Metal = ResourceStorageRules.ApplyProductionLimit(planetState.Metal, MetalProductionRate, elapsedSeconds, storageCapacities.Metal);
        planetState.Crystal = ResourceStorageRules.ApplyProductionLimit(planetState.Crystal, CrystalProductionRate, elapsedSeconds, storageCapacities.Crystal);
        planetState.Deuterium = ResourceStorageRules.ApplyProductionLimit(planetState.Deuterium, DeuteriumProductionRate, elapsedSeconds, storageCapacities.Deuterium);
        planetState.LastResourceUpdate = now;
    }

    public async Task SettleActivePlanetResourcesAsync(bool notifyStateChanged = false)
    {
        var state = await GetActivePlanetStateAsync();
        var storageCapacities = await GetActivePlanetStorageCapacitiesAsync();
        SettlePlanetResourcesToNow(state, storageCapacities, DateTime.UtcNow);
        await _dbContext.SaveChangesAsync();

        if (notifyStateChanged)
        {
            NotifyStateChanged();
        }
    }

    public async Task<bool> HasResourcesAsync(long metal, long crystal, long deuterium)
    {
        var gameState = await GetStateAsync();
        var planetState = await GetActivePlanetStateAsync();
        var storageCapacities = await GetActivePlanetStorageCapacitiesAsync();
        var snapshot = BuildPreviewSnapshot(gameState, planetState, storageCapacities, DateTime.UtcNow);
        return snapshot.Metal >= metal && snapshot.Crystal >= crystal && snapshot.Deuterium >= deuterium;
    }

    public async Task<bool> ConsumeResourcesAsync(long metal, long crystal, long deuterium)
    {
        if (!await HasResourcesAsync(metal, crystal, deuterium))
        {
            return false;
        }

        var state = await GetActivePlanetStateAsync();
        var storageCapacities = await GetActivePlanetStorageCapacitiesAsync();
        SettlePlanetResourcesToNow(state, storageCapacities, DateTime.UtcNow);
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
        var storageCapacities = await GetActivePlanetStorageCapacitiesAsync();
        SettlePlanetResourcesToNow(state, storageCapacities, DateTime.UtcNow);
        state.Metal += metal;
        state.Crystal += crystal;
        state.Deuterium += deuterium;

        await _dbContext.SaveChangesAsync();
        NotifyStateChanged();
    }

    public async Task AddResourcesToPlanetAsync(int g, int s, int p, double metal, double crystal, double deuterium)
    {
        var state = await GetPlanetStateAsync(g, s, p);
        var storageCapacities = await GetPlanetStorageCapacitiesAsync(g, s, p);
        SettlePlanetResourcesToNow(state, storageCapacities, DateTime.UtcNow);
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

    private async Task<ResourceStorageCapacities> GetActivePlanetStorageCapacitiesAsync()
    {
        return await GetPlanetStorageCapacitiesAsync(_playerStateService.ActiveGalaxy, _playerStateService.ActiveSystem, _playerStateService.ActivePosition);
    }

    private async Task<ResourceStorageCapacities> GetPlanetStorageCapacitiesAsync(int g, int s, int p)
    {
        var storageLevels = await _dbContext.Buildings
            .Where(b => b.Galaxy == g && b.System == s && b.Position == p)
            .Where(b => b.BuildingType == MetalStorageBuilding || b.BuildingType == CrystalStorageBuilding || b.BuildingType == DeuteriumStorageBuilding)
            .ToDictionaryAsync(b => b.BuildingType, b => b.Level);

        return new ResourceStorageCapacities(
            ResourceStorageRules.CalculateCapacity(storageLevels.GetValueOrDefault(MetalStorageBuilding, 0)),
            ResourceStorageRules.CalculateCapacity(storageLevels.GetValueOrDefault(CrystalStorageBuilding, 0)),
            ResourceStorageRules.CalculateCapacity(storageLevels.GetValueOrDefault(DeuteriumStorageBuilding, 0))
        );
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
public record ResourceStorageCapacities(long Metal, long Crystal, long Deuterium);
public record ResourceDisplayState(ResourceSnapshot Snapshot, ResourceStorageCapacities StorageCapacities);

public static class ResourceStorageRules
{
    private const long BaseCapacity = 1_000_000;
    private const double GrowthFactor = 1.68;

    public static long CalculateCapacity(int level)
    {
        level = Math.Max(0, level);
        double rawCapacity = BaseCapacity * Math.Pow(GrowthFactor, level);
        return RoundDownToThreeSignificantDigits(rawCapacity);
    }

    public static double ApplyProductionLimit(double currentAmount, double productionRate, double elapsedSeconds, long storageCapacity)
    {
        if (elapsedSeconds <= 0 || productionRate <= 0) return currentAmount;
        if (currentAmount >= storageCapacity) return currentAmount;

        return Math.Min(currentAmount + (productionRate * elapsedSeconds), storageCapacity);
    }

    private static long RoundDownToThreeSignificantDigits(double value)
    {
        if (value <= 0) return 0;

        int digits = (int)Math.Floor(Math.Log10(value)) + 1;
        int shift = Math.Max(0, digits - 3);
        double factor = Math.Pow(10, shift);
        return (long)(Math.Floor(value / factor) * factor);
    }
}
