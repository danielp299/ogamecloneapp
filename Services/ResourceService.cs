namespace myapp.Services;

public class ResourceService
{
    private double _metal = 50000;
    private double _crystal = 50000;
    private double _deuterium = 50000;
    private double _darkMatter = 0;

    public long Metal => (long)_metal;
    public long Crystal => (long)_crystal;
    public long Deuterium => (long)_deuterium;
    public long DarkMatter => (long)_darkMatter;
    public long Energy { get; private set; } = 0;

    public DateTime LastUpdate { get; private set; } = DateTime.UtcNow;

    public event Action OnChange;

    public void UpdateResources(double metalRate, double crystalRate, double deuteriumRate)
    {
        var now = DateTime.UtcNow;
        var elapsedSeconds = (now - LastUpdate).TotalSeconds;

        if (elapsedSeconds > 0)
        {
            _metal += metalRate * elapsedSeconds;
            _crystal += crystalRate * elapsedSeconds;
            _deuterium += deuteriumRate * elapsedSeconds;
            LastUpdate = now;
            NotifyStateChanged();
        }
    }

    public bool HasResources(long metal, long crystal, long deuterium)
    {
        // Force update before checking (requires rates, but this method is passive check)
        // Ideally we update resources before any check/consume action. 
        // For now, checks are against CURRENT state.
        return Metal >= metal && Crystal >= crystal && Deuterium >= deuterium;
    }

    public bool ConsumeResources(long metal, long crystal, long deuterium)
    {
        if (!HasResources(metal, crystal, deuterium))
        {
            return false;
        }

        _metal -= metal;
        _crystal -= crystal;
        _deuterium -= deuterium;
        NotifyStateChanged();
        return true;
    }

    public void AddResources(double metal, double crystal, double deuterium)
    {
        _metal += metal;
        _crystal += crystal;
        _deuterium += deuterium;
        NotifyStateChanged();
    }

    public void AddDarkMatter(long amount)
    {
        _darkMatter += amount;
        NotifyStateChanged();
    }

    // Energy is usually a calculated state (Production - Consumption), 
    // but for now we allow direct modification or setting it.
    public void SetEnergy(long energy)
    {
        Energy = energy;
        NotifyStateChanged();
    }
    
    public void UpdateEnergy(long deltaEnergy)
    {
        Energy += deltaEnergy;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
