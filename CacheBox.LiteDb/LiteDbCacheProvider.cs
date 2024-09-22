using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CacheBox.LiteDb;

/// <inheritdoc/>
/// <remarks>
/// This implementation uses LiteDb as the storage for cached items.
/// </remarks>
public class LiteDbCacheProvider : ICacheProvider
{
    private readonly ILogger<LiteDbCacheProvider> _logger;
    private readonly CacheProviderConfig _config;
    private readonly ILiteCollection<CacheRecord>? _collection;

    /// <inheritdoc/>
    public bool IsConnected => _collection is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiteDbCacheProvider" class./>
    /// This constructor is intended for use by Dependency Injection and should be used in conjunction with the
    /// <see cref="StartupExtensions.AddCacheProvider{TProvider}(Microsoft.Extensions.Hosting.IHostApplicationBuilder)"/> method and not called directly.
    /// </summary>
    /// <param name="config">The application's configuration settings.</param>
    /// <param name="logger">The logger instance for logging cache operations.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration of the cache provider is not provided or is invalid.
    /// </exception>
    public LiteDbCacheProvider(IConfiguration config, ILogger<LiteDbCacheProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        _config = config.GetSection(CacheConstants.ConfigurationSection).Get<CacheProviderConfig>() ?? throw new InvalidOperationException(CacheConstants.ConfigurationSectionError);
        _config.AppPrefix = _config.AppPrefix + ":" ?? string.Empty;

        if (string.IsNullOrEmpty(_config.ConnectionString))
        {
            _logger.LogError($"{nameof(LiteDbCacheProvider)}: LiteDb is not configured properly. Caching disabled.");
            return;
        }

        try
        {
            _logger.LogDebug("Attempting LiteDb connection.");
            LiteDatabase db = new(_config.ConnectionString);
            _collection = db.GetCollection<CacheRecord>();
            _collection.EnsureIndex(a => a.Key, true);
            _collection.EnsureIndex(a => a.ValidUntil);
            RemoveOldCacheValues();
            _logger.LogDebug("Connected to LiteDb");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(LiteDbCacheProvider)}: Could not establish connection. Caching disabled.");
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
        CacheRecord value = _collection.FindOne(a => a.Key.Equals(fullKey));
        if (value is null || value.ValidUntil < DateTimeOffset.UtcNow)
        {
            _collection.DeleteMany(a => a.Key.Equals(fullKey));
            return default;
        }
        if (typeof(T) == typeof(string)) return (T)(object)value.Value.ToString();

        T? tVal = System.Text.Json.JsonSerializer.Deserialize<T>(value.Value!);
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
        _collection.DeleteMany(a => a.Key.Equals(fullKey));
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
            _collection.Upsert(new CacheRecord(fullKey, (string)(object)value, DateTimeOffset.UtcNow.Add(timeout.Value)));
        }
        else
        {
            _collection.Upsert(new CacheRecord(fullKey, System.Text.Json.JsonSerializer.Serialize(value), DateTimeOffset.UtcNow.Add(timeout.Value)));
        }
    }

    private void RemoveOldCacheValues()
    {
        if (_collection is null) return;
        _collection.DeleteMany(a => a.ValidUntil < DateTimeOffset.UtcNow);
    }
}