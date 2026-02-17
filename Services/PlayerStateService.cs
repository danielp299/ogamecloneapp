namespace myapp.Services;

public class PlayerStateService
{
    private readonly GalaxyService _galaxyService;
    
    public int ActiveGalaxy { get; private set; }
    public int ActiveSystem { get; private set; }
    public int ActivePosition { get; private set; }

    public event Action? OnChange;

    private bool _isInitialized = false;

    public PlayerStateService(GalaxyService galaxyService)
    {
        _galaxyService = galaxyService;
        // NOTA: La inicialización es lazy via Initialize()
    }

    public void Initialize()
    {
        if (_isInitialized) return;
        
        // Initialize with home planet
        ActiveGalaxy = _galaxyService.HomeGalaxy;
        ActiveSystem = _galaxyService.HomeSystem;
        ActivePosition = _galaxyService.HomePosition;
        
        _isInitialized = true;
        Console.WriteLine($"PlayerStateService initialized with planet: {ActiveGalaxy}:{ActiveSystem}:{ActivePosition}");
    }

    public void SetActivePlanet(int g, int s, int p)
    {
        ActiveGalaxy = g;
        ActiveSystem = s;
        ActivePosition = p;
        OnChange?.Invoke();
    }

    public void ResetState()
    {
        ActiveGalaxy = 0;
        ActiveSystem = 0;
        ActivePosition = 0;
        _isInitialized = false;
    }
}
