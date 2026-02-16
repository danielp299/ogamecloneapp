using Microsoft.EntityFrameworkCore;
using myapp.Data;

namespace myapp.Services;

public class DevModeService
{
    private readonly GameDbContext _dbContext;
    private bool _isEnabled = true;
    
    public event Action? OnChange;

    public DevModeService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public bool IsEnabled => _isEnabled;

    public async Task LoadStateAsync()
    {
        var state = await _dbContext.GameState.FirstOrDefaultAsync();
        if (state != null)
        {
            _isEnabled = state.DevModeEnabled;
        }
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        _isEnabled = enabled;
        
        var state = await _dbContext.GameState.FirstOrDefaultAsync();
        if (state != null)
        {
            state.DevModeEnabled = enabled;
            await _dbContext.SaveChangesAsync();
        }
        
        NotifyStateChanged();
    }

    public async Task ToggleAsync()
    {
        await SetEnabledAsync(!_isEnabled);
    }

    // Helper to get dev time if enabled, or original time if not
    public TimeSpan GetDuration(TimeSpan original, int devSeconds = 5)
    {
        return _isEnabled ? TimeSpan.FromSeconds(devSeconds) : original;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
