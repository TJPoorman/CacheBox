using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CacheBox.Memory;

/// <inheritdoc/>
/// <remarks>
/// This implementation uses in-memory as the storage for cached items.
/// </remarks>
public class MemoryCacheProvider : ICacheProvider
{
    private readonly ILogger<MemoryCacheProvider> _logger;
    private readonly CacheProviderConfig _config;
    private readonly ConcurrentDictionary<string, CacheRecord>? _collection;
    private readonly Timer? _cleanUpTimer;
    private readonly static SemaphoreSlim _globalStaticLock = new(1);

    /// <inheritdoc/>
    public bool IsConnected => _collection is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheProvider" class./>
    /// This constructor is intended for use by Dependency Injection and should be used in conjunction with the
    /// <see cref="StartupExtensions.AddCacheProvider{TProvider}(Microsoft.Extensions.Hosting.IHostApplicationBuilder)"/> method and not called directly.
    /// </summary>
    /// <param name="config">The application's configuration settings.</param>
    /// <param name="logger">The logger instance for logging cache operations.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration of the cache provider is not provided or is invalid.
    /// </exception>
    public MemoryCacheProvider(IConfiguration config, ILogger<MemoryCacheProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        _config = config.GetSection(CacheConstants.ConfigurationSection).Get<CacheProviderConfig>() ?? throw new InvalidOperationException(CacheConstants.ConfigurationSectionError);
        _config.AppPrefix = _config.AppPrefix + ":" ?? string.Empty;
        
        try
        {
            _logger.LogDebug("Configuring memory cache.");
            _collection = new();
            _cleanUpTimer = new Timer(s => { _ = RemoveExpiredJob(); }, null, 10000, 10000);
            _logger.LogDebug("Memory cache successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(MemoryCacheProvider)}: Could not establish connection. Caching disabled.");
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetAsync(string key, string? callerPrefix = null) => await GetAsync<string>(key, callerPrefix);

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key, string? callerPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_collection is null) throw CacheConstants.NotConnectedException;

        await Task.FromResult(0);

        string fullKey = $"{_config.AppPrefix}{callerPrefix?.IfNotNull($"{callerPrefix}:")}{key}";
        _collection.TryGetValue(fullKey, out CacheRecord? value);
        if (value is null || value.ValidUntil < DateTimeOffset.UtcNow)
        {
            _collection.TryRemove(fullKey, out _);
            return default;
        }
        if (typeof(T) == typeof(string)) return (T)(object)value.Value.ToString();

        T? tVal = JsonSerializer.Deserialize<T>(value.Value!);
        if (tVal is null) return default;
        return tVal;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveAsync(string key, string? callerPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_collection is null) throw CacheConstants.NotConnectedException;

        await Task.FromResult(0);

        string fullKey = $"{_config.AppPrefix}{callerPrefix?.IfNotNull($"{callerPrefix}:")}{key}";
        _collection.TryRemove(fullKey, out _);
        return true;
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, string? callerPrefix = null) => await SetAsync<T>(key, value, _config.TimeoutTimeSpan, callerPrefix);

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan? timeout, string? callerPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        if (_collection is null) throw CacheConstants.NotConnectedException;

        await Task.FromResult(0);

        string fullKey = $"{_config.AppPrefix}{callerPrefix?.IfNotNull($"{callerPrefix}:")}{key}";
        timeout ??= TimeSpan.MaxValue;

        if (typeof(T) == typeof(string))
        {
            CacheRecord rec = new(string.Empty, (string)(object)value, DateTimeOffset.UtcNow.Add(timeout.Value));
            _collection.AddOrUpdate(fullKey, rec, (k, v) => v = rec);
        }
        else
        {
            CacheRecord rec = new(string.Empty, JsonSerializer.Serialize(value), DateTimeOffset.UtcNow.Add(timeout.Value));
            _collection.AddOrUpdate(fullKey, rec, (k, v) => v = rec);
        }
    }

    private async Task RemoveExpiredJob()
    {
        await _globalStaticLock.WaitAsync().ConfigureAwait(false);
        try
        {
            RemoveExpired();
        }
        finally { _globalStaticLock.Release(); }
    }

    private void RemoveExpired()
    {
        if (_collection is null || _cleanUpTimer is null) return;

        if (Monitor.TryEnter(_cleanUpTimer))
        {
            try
            {
                var currTime = DateTimeOffset.UtcNow;
                foreach (var p in _collection)
                {
                    if (currTime > p.Value.ValidUntil) _collection.TryRemove(p);
                }
            }
            finally
            {
                Monitor.Exit(_cleanUpTimer);
            }
        }
    }
}