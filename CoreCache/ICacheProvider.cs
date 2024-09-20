namespace CoreCache;

public interface ICacheProvider
{
    bool IsConnected { get; }

    Task<string?> GetAsync(string key, string? callerPrefix = null);
    Task<T?> GetAsync<T>(string key, string? callerPrefix = null);
    Task<bool> RemoveAsync(string key, string? callerPrefix = null);
    Task SetAsync<T>(string key, T value, string? callerPrefix = null);
    Task SetAsync<T>(string key, T value, TimeSpan? timeout, string? callerPrefix = null);
}
