using System.Text.Json;
using Dapper;

namespace Leaf.Data;

public sealed class SettingsStore(Database db)
{
    private SiteSettings? _cache;

    public SiteSettings Get()
    {
        if (_cache is not null) return _cache;
        using var conn = db.Open();
        var json = conn.QuerySingleOrDefault<string>("SELECT value FROM settings WHERE key = 'site'");
        return _cache = json is null ? new SiteSettings() : JsonSerializer.Deserialize<SiteSettings>(json, JsonSerializerOptions.Web)!;
    }

    public void Save(SiteSettings settings)
    {
        using var conn = db.Open();
        conn.Execute("""
            INSERT INTO settings (key, value) VALUES ('site', @json)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value
            """, new { json = JsonSerializer.Serialize(settings, JsonSerializerOptions.Web) });
        _cache = settings;
    }
}
