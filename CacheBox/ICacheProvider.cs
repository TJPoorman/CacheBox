namespace CacheBox;

/// <summary>
/// Interface for caching provider used to store, retrieve, and remove data in a cache.
/// </summary>
public interface ICacheProvider
{
    /// <summary>
    /// Gets a value indicating whether the connection to the provider is currently active.
    /// Returns true if connected; otherwise, false.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Retrieves a string value from the cache by key and optional prefix.
    /// </summary>
    /// <param name="key">The key of the item to retrieve.</param>
    /// <param name="callerPrefix">
    /// Optional value used to separate multiple possible keys with the same name.
    /// Ex: Use nameof(MyClass) to separate by class.
    /// </param>
    /// <returns>The string value of the cached item if it exists; otherwise, null.</returns>
    Task<string?> GetAsync(string key, string? callerPrefix = null);

    /// <summary>
    /// Retrieves an item from the cache by key and optional prefix.
    /// </summary>
    /// <typeparam name="T">The type of the item to retrieve.</typeparam>
    /// <param name="key">The key of the item to retrieve.</param>
    /// <param name="callerPrefix">
    /// Optional value used to separate multiple possible keys with the same name.
    /// Ex: Use nameof(MyClass) to separate by class.
    /// </param>
    /// <returns>The cached item of type <typeparamref name="T"/> if it exists; otherwise, default(T).</returns>
    Task<T?> GetAsync<T>(string key, string? callerPrefix = null);

    /// <summary>
    /// Removes an item from the cache by key and optional prefix.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    /// <param name="callerPrefix">
    /// Optional value used to separate multiple possible keys with the same name.
    /// Ex: Use nameof(MyClass) to separate by class.
    /// </param>
    /// <returns>Returns true if the item was removed; otherwise, false.</returns>
    Task<bool> RemoveAsync(string key, string? callerPrefix = null);

    /// <summary>
    /// Stores an item in the cache by key and optional prefix.
    /// </summary>
    /// <typeparam name="T">The type of the item to store.</typeparam>
    /// <param name="key">The key of the item to store.</param>
    /// <param name="value">The value of the item to store.</param>
    /// <param name="callerPrefix">
    /// Optional value used to separate multiple possible keys with the same name.
    /// Ex: Use nameof(MyClass) to separate by class.
    /// </param>
    Task SetAsync<T>(string key, T value, string? callerPrefix = null);

    /// <summary>
    /// Stores an item in the cache by key and optional prefix.
    /// </summary>
    /// <typeparam name="T">The type of the item to store.</typeparam>
    /// <param name="key">The key of the item to store.</param>
    /// <param name="value">The value of the item to store.</param>
    /// <param name="timeout">
    /// The time for the item to expire. If null, will attempt to use default
    /// from config; otherwise will never expire.
    /// </param>
    /// <param name="callerPrefix">
    /// Optional value used to separate multiple possible keys with the same name.
    /// Ex: Use nameof(MyClass) to separate by class.
    /// </param>
    Task SetAsync<T>(string key, T value, TimeSpan? timeout, string? callerPrefix = null);
}
