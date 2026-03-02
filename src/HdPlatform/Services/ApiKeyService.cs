using HdPlatform.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace HdPlatform.Services;

public class ApiKeyService
{
    private readonly string _keysPath;
    private readonly string _usagePath;
    private readonly ConcurrentDictionary<string, ApiKeyInfo> _keys = new();
    private readonly ConcurrentDictionary<string, int> _monthlyUsage = new();
    private readonly object _saveLock = new();
    private readonly ILogger<ApiKeyService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiKeyService(IConfiguration config, ILogger<ApiKeyService> logger)
    {
        _logger = logger;
        var dataDir = config.GetValue<string>("DataDirectory") ?? "/app/data";
        Directory.CreateDirectory(dataDir);
        _keysPath = Path.Combine(dataDir, "api-keys.json");
        _usagePath = Path.Combine(dataDir, "usage.json");
        Load();
    }

    public ApiKeyInfo? Validate(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey)) return null;
        return _keys.TryGetValue(apiKey, out var info) && info.Active ? info : null;
    }

    public (bool allowed, int used, int limit) CheckRateLimit(string apiKey)
    {
        var info = Validate(apiKey);
        if (info == null) return (false, 0, 0);
        
        var usageKey = $"{apiKey}:{DateTime.UtcNow:yyyy-MM}";
        var used = _monthlyUsage.GetOrAdd(usageKey, 0);
        return (used < info.MonthlyLimit, used, info.MonthlyLimit);
    }

    public void RecordUsage(string apiKey)
    {
        var usageKey = $"{apiKey}:{DateTime.UtcNow:yyyy-MM}";
        _monthlyUsage.AddOrUpdate(usageKey, 1, (_, c) => c + 1);
        _ = Task.Run(SaveUsage); // Save async to avoid blocking
    }

    public ApiKeyInfo CreateKey(string name, string email, string tier = "free")
    {
        var key = $"hd_{tier}_{Guid.NewGuid():N}"[..32];
        var info = new ApiKeyInfo(key, name, email, tier, DateTime.UtcNow, true);
        _keys[key] = info;
        _ = Task.Run(SaveKeys); // Save async
        _logger.LogInformation("Created API key {Tier} for {Name} <{Email}>", tier, name, email);
        return info;
    }

    public IReadOnlyList<ApiKeyInfo> ListKeys() => _keys.Values.ToList();

    public Dictionary<string, int> GetUsage(string apiKey) =>
        _monthlyUsage.Where(kv => kv.Key.StartsWith(apiKey + ":"))
            .ToDictionary(kv => kv.Key.Split(':')[1], kv => kv.Value);

    private void Load()
    {
        try
        {
            if (File.Exists(_keysPath))
            {
                var keys = JsonSerializer.Deserialize<List<ApiKeyInfo>>(File.ReadAllText(_keysPath), JsonOpts);
                if (keys != null)
                {
                    foreach (var k in keys) _keys[k.Key] = k;
                    _logger.LogInformation("Loaded {Count} API keys", keys.Count);
                }
            }

            if (File.Exists(_usagePath))
            {
                var usage = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(_usagePath), JsonOpts);
                if (usage != null)
                {
                    foreach (var kv in usage) _monthlyUsage[kv.Key] = kv.Value;
                    _logger.LogInformation("Loaded usage data for {Count} entries", usage.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API key data");
        }
    }

    private void SaveKeys()
    {
        try
        {
            lock (_saveLock)
            {
                File.WriteAllText(_keysPath, JsonSerializer.Serialize(_keys.Values.ToList(), JsonOpts));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving API keys");
        }
    }

    private void SaveUsage()
    {
        try
        {
            lock (_saveLock)
            {
                File.WriteAllText(_usagePath, JsonSerializer.Serialize(_monthlyUsage.ToDictionary(kv => kv.Key, kv => kv.Value), JsonOpts));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving usage data");
        }
    }
}