using System;

namespace myapp.Services;

public class DevModeService
{
    public bool IsEnabled { get; private set; } = true;
    public event Action OnChange;

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        NotifyStateChanged();
    }

    public void Toggle()
    {
        IsEnabled = !IsEnabled;
        NotifyStateChanged();
    }

    // Helper to get dev time if enabled, or original time if not
    public TimeSpan GetDuration(TimeSpan original, int devSeconds = 5)
    {
        return IsEnabled ? TimeSpan.FromSeconds(devSeconds) : original;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
