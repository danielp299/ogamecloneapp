namespace myapp.Services;

public class PlayerStateService
{
    private readonly GalaxyService _galaxyService;
    
    public int ActiveGalaxy { get; private set; }
    public int ActiveSystem { get; private set; }
    public int ActivePosition { get; private set; }

    public event Action? OnChange;

    public PlayerStateService(GalaxyService galaxyService)
    {
        _galaxyService = galaxyService;
        
        // Initialize with home planet
        ActiveGalaxy = _galaxyService.HomeGalaxy;
        ActiveSystem = _galaxyService.HomeSystem;
        ActivePosition = _galaxyService.HomePosition;
    }

    public void SetActivePlanet(int g, int s, int p)
    {
        ActiveGalaxy = g;
        ActiveSystem = s;
        ActivePosition = p;
        OnChange?.Invoke();
    }
}
