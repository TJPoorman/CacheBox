namespace CoreCache;

internal static class CacheConstants
{
    public static string ConfigurationSection = "Cache";
    public static string ConfigurationSectionError = $"Configuration section '{ConfigurationSection}' is missing or invalid.";
    public static Exception NotConnectedException = new("Cache provider is not connected.");
}
