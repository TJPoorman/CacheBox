using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace CacheBox.Redis;

/// <inheritdoc/>
/// <remarks>
/// This implementation uses Redis as the storage for cached items.
/// </remarks>
public class RedisCacheProvider : ICacheProvider
{
    private readonly ILogger<RedisCacheProvider> _logger;
    private readonly CacheProviderConfig _config;
    private readonly IDatabaseAsync? _database;

    /// <inheritdoc/>
    public bool IsConnected => _database is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCacheProvider" class./>
    /// This constructor is intended for use by Dependency Injection and should be used in conjunction with the
    /// <see cref="StartupExtensions.AddCacheProvider{TProvider}(Microsoft.Extensions.Hosting.IHostApplicationBuilder)"/> method and not called directly.
    /// </summary>
    /// <param name="config">The application's configuration settings.</param>
    /// <param name="logger">The logger instance for logging cache operations.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration of the cache provider is not provided or is invalid.
    /// </exception>
    public RedisCacheProvider(IConfiguration config, ILogger<RedisCacheProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        _config = config.GetSection(CacheConstants.ConfigurationSection).Get<CacheProviderConfig>() ?? throw new InvalidOperationException(CacheConstants.ConfigurationSectionError);
        _config.AppPrefix = _config.AppPrefix + ":" ?? string.Empty;

        if (string.IsNullOrEmpty(_config.ConnectionString))
        {
            _logger.LogError($"{nameof(RedisCacheProvider)}: Redis is not configured properly. Caching disabled.");
            return;
        }

        try
        {
            _logger.LogDebug("Attempting Redis connection.");
            ConnectionMultiplexer conn = ConnectionMultiplexer.Connect(_config.ConnectionString);
            _database = conn.GetDatabase();
            _logger.LogDebug("Connected to Redis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(RedisCacheProvider)}: Could not establish connection. Caching disabled.");
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetAsync(string key, string? callerPrefix = null) => await GetAsync<string>(key, callerPrefix);

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key, string? callerPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_database is null) throw CacheConstants.NotConnectedException;

        RedisValue value = await _database.StringGetAsync($"{_config.AppPrefix}{callerPrefix?.IfNotNull($"{callerPrefix}:")}{key}");
        if (value.IsNull) return default;
        if (typeof(T) == typeof(string)) return (T)(object)value.ToString();

        T? tVal = JsonSerializer.Deserialize<T>(value!);
        if (tVal is null) return default;
        return tVal;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveAsync(string key, string? callerPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_database is null) throw CacheConstants.NotConnectedException;

        await _database.KeyDeleteAsync($"{_config.AppPrefix}{callerPrefix?.IfNotNull($"{callerPrefix}:")}{key}", CommandFlags.FireAndForget);
        return true;
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, string? callerPrefix = null) => await SetAsync<T>(key, value, _config.TimeoutTimeSpan, callerPrefix);

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan? timeout, string? callerPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        if (_database is null) throw CacheConstants.NotConnectedException;

        if (typeof(T) == typeof(string))
        {
            await _database.StringSetAsync($"{_config.AppPrefix}{callerPrefix?.IfNotNull($"{callerPrefix}:")}{key}", (string)(object)value, timeout, when: When.Always);
        }
        else
        {
            await _database.StringSetAsync($"{_config.AppPrefix}{callerPrefix?.IfNotNull($"{callerPrefix}:")}{key}", JsonSerializer.Serialize(value), timeout, when: When.Always);
        }
    }
}