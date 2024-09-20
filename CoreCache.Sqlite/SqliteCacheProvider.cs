using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreCache.Sqlite;

public class SqliteCacheProvider : ICacheProvider
{
    private readonly ILogger<SqliteCacheProvider> _logger;
    private readonly CacheProviderConfig _config;
    private readonly SqliteConnection? _connection;

    public bool IsConnected => _connection is not null;

    public SqliteCacheProvider(IConfiguration config, ILogger<SqliteCacheProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        _config = config.GetSection(CacheConstants.ConfigurationSection).Get<CacheProviderConfig>() ?? throw new InvalidOperationException(CacheConstants.ConfigurationSectionError);
        _config.AppPrefix = _config.AppPrefix + ":" ?? string.Empty;

        if (string.IsNullOrEmpty(_config.ConnectionString))
        {
            _logger.LogError($"{nameof(SqliteCacheProvider)}: Sqlite is not configured properly. Caching disabled.");
            return;
        }

        try
        {
            _logger.LogDebug("Attempting Sqlite connection.");
            _connection = new(_config.ConnectionString);
            _connection.Open();
            var command = _connection.CreateCommand();
            command.CommandText = "CREATE TABLE IF NOT EXISTS cache ( Key PRIMARY KEY, Value TEXT NOT NULL, ValidUntil TEXT NOT NULL) WITHOUT ROWID";
            command.ExecuteNonQuery();
            RemoveOldCacheValues();
            _logger.LogDebug("Connected to Sqlite");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(SqliteCacheProvider)}: Could not establish connection. Caching disabled.");
        }
    }

    public async Task<string?> GetAsync(string key, string? callerPrefix = null) => await GetAsync<string>(key, callerPrefix);

    public async Task<T?> GetAsync<T>(string key, string? callerPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_connection is null) throw CacheConstants.NotConnectedException;

        string fullKey = $"{_config.AppPrefix}{callerPrefix?.IfNotNull($"{callerPrefix}:")}{key}";

        var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM cache WHERE Key = $key";
        command.Parameters.AddWithValue("$key", fullKey);
        using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return default;

        CacheRecord value = new(reader.GetString(0), reader.GetString(1), DateTimeOffset.Parse(reader.GetString(2)));
        if (value is null || value.ValidUntil < DateTimeOffset.UtcNow)
        {
            var delcommand = _connection.CreateCommand();
            delcommand.CommandText = "DELETE FROM cache WHERE Key = $key";
            delcommand.Parameters.AddWithValue("$key", fullKey);
            await delcommand.ExecuteNonQueryAsync();
            return default;
        }
        if (typeof(T) == typeof(string)) return (T)(object)value.Value.ToString();

        T? tVal = System.Text.Json.JsonSerializer.Deserialize<T>(value.Value!);
        if (tVal is null) return default;
        return tVal;
    }

    public async Task<bool> RemoveAsync(string key, string? callerPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_connection is null) throw CacheConstants.NotConnectedException;

        string fullKey = $"{_config.AppPrefix}{callerPrefix?.IfNotNull($"{callerPrefix}:")}{key}";
        var delcommand = _connection.CreateCommand();
        delcommand.CommandText = "DELETE FROM cache WHERE Key = $key";
        delcommand.Parameters.AddWithValue("$key", fullKey);
        await delcommand.ExecuteNonQueryAsync();

        return true;
    }

    public async Task SetAsync<T>(string key, T value, string? callerPrefix = null) => await SetAsync<T>(key, value, _config.TimeoutTimeSpan, callerPrefix);

    public async Task SetAsync<T>(string key, T value, TimeSpan? timeout, string? callerPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        if (_connection is null) throw CacheConstants.NotConnectedException;

        await Task.FromResult(0);

        string fullKey = $"{_config.AppPrefix}{callerPrefix?.IfNotNull($"{callerPrefix}:")}{key}";
        string insert = "INSERT INTO cache (Key, Value, ValidUntil) VALUES ($key, $value, $validUntil)";
        string update = "UPDATE cache SET Value = $value, ValidUntil = $validUntil WHERE Key = $key";

        var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM cache WHERE Key = $key";
        command.Parameters.AddWithValue("$key", fullKey);
        bool keyExists = ((int?)await command.ExecuteScalarAsync()) > 0;
        timeout ??= TimeSpan.MaxValue;
        string dbVal;

        if (typeof(T) == typeof(string))
        {
            dbVal = (string)(object)value;
        }
        else
        {
            dbVal = System.Text.Json.JsonSerializer.Serialize(value);
        }

        command = _connection.CreateCommand();
        command.CommandText = keyExists ? update : insert;
        command.Parameters.AddWithValue("$key", fullKey);
        command.Parameters.AddWithValue("$value", dbVal);
        command.Parameters.AddWithValue("$validUntil", DateTimeOffset.UtcNow.Add(timeout.Value).ToString());
        await command.ExecuteNonQueryAsync();
    }

    private void RemoveOldCacheValues()
    {
        if (_connection is null) return;

        var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM cache WHERE ValidUntil < $validUntil";
        command.Parameters.AddWithValue("$validUntil", DateTimeOffset.UtcNow);
        command.ExecuteNonQuery();
    }
}