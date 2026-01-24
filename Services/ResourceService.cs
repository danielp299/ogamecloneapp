namespace myapp.Services;

public class ResourceService
{
    private double _metal = 50000;
    private double _crystal = 50000;
    private double _deuterium = 50000;

    public long Metal => (long)_metal;
    public long Crystal => (long)_crystal;
    public long Deuterium => (long)_deuterium;
    public long Energy { get; private set; } = 0;

    public event Action OnChange;

    public bool HasResources(long metal, long crystal, long deuterium)
    {
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

    // Energy is usually a calculated state (Production - Consumption), 
    // but for now we allow direct modification or setting it.
    public void UpdateEnergy(long deltaEnergy)
    {
        Energy += deltaEnergy;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
