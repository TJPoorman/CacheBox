namespace SimpleCache;

internal class CacheProviderConfig
{
    public string? AppPrefix { get; set; }
    public string? ConnectionString { get; set; }
    public string? Timeout {  get; set; }
    public TimeSpan? TimeoutTimeSpan => TimeSpan.TryParse(Timeout, out TimeSpan t) ? t : null;
}
