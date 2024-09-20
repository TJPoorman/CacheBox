namespace CacheBox;

internal class CacheRecord
{
    public string Key { get; set; }
    public string Value { get; set; }
    public DateTimeOffset ValidUntil { get; set; }

    public CacheRecord(string key, string value, DateTimeOffset validUntil)
    {
        Key = key;
        Value = value;
        ValidUntil = validUntil;
    }
}