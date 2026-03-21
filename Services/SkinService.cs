namespace myapp.Services;

/// <summary>
/// Skin configuration. A skin is a folder name under wwwroot/assets/.
/// Change CurrentSkin to switch all asset paths at once.
/// </summary>
public static class SkinConfig
{
    public static string CurrentSkin { get; set; } = "skin1";

    /// <summary>Returns the full asset URL for a path relative to the skin folder.</summary>
    /// <param name="relativePath">e.g. "buildings/building1.jpg"</param>
    public static string Url(string relativePath) => $"assets/{CurrentSkin}/{relativePath}";
}
