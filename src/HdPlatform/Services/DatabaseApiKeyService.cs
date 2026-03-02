using HdPlatform.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HdPlatform.Services;

public class DatabaseApiKeyService
{
    private readonly HdPlatformContext _context;
    private readonly ILogger<DatabaseApiKeyService> _logger;

    public DatabaseApiKeyService(HdPlatformContext context, ILogger<DatabaseApiKeyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiKey> CreateKeyAsync(string name, string email, string tier = "free")
    {
        // Check if email already has an API key
        var existingKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Email == email && k.Active);
            
        if (existingKey != null)
        {
            _logger.LogInformation("Returning existing API key for email {Email}", email);
            return existingKey;
        }

        var (monthlyLimit, revenue) = tier switch
        {
            "pro" => (2000, 29.00m),
            "business" => (100000, 99.00m),
            _ => (50, 0.00m)
        };

        var apiKey = new ApiKey
        {
            Key = GenerateApiKey(tier),
            Name = name,
            Email = email,
            Tier = tier,
            Active = true,
            MonthlyLimit = monthlyLimit,
            MonthlyRevenue = revenue,
            CurrentMonthUsage = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created API key {Tier} for {Name} <{Email}>", tier, name, email);
        return apiKey;
    }

    public async Task<ApiKey?> ValidateKeyAsync(string key)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Key == key && k.Active);

        if (apiKey == null) return null;

        // Reset usage counter if new month
        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var lastUsedMonth = apiKey.LastUsed.HasValue 
            ? new DateOnly(apiKey.LastUsed.Value.Year, apiKey.LastUsed.Value.Month, 1)
            : DateOnly.MinValue;

        if (currentMonth != lastUsedMonth)
        {
            apiKey.CurrentMonthUsage = 0;
        }

        return apiKey;
    }

    public async Task<bool> CheckRateLimitAsync(string key)
    {
        var apiKey = await ValidateKeyAsync(key);
        if (apiKey == null) return false;

        return apiKey.CurrentMonthUsage < apiKey.MonthlyLimit;
    }

    public async Task IncrementUsageAsync(string key, string endpoint, int responseTimeMs, bool success, string? errorMessage = null, string? ipAddress = null, string? userAgent = null)
    {
        var apiKey = await ValidateKeyAsync(key);
        if (apiKey == null) return;

        // Update API key usage
        apiKey.CurrentMonthUsage++;
        apiKey.LastUsed = DateTime.UtcNow;

        // Log detailed usage
        var usage = new ApiUsage
        {
            ApiKeyId = apiKey.Id,
            Endpoint = endpoint,
            Timestamp = DateTime.UtcNow,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ResponseTimeMs = responseTimeMs,
            Success = success,
            ErrorMessage = errorMessage,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Length > 255 ? userAgent[..255] : userAgent
        };

        _context.ApiUsage.Add(usage);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ApiKey>> ListKeysAsync()
    {
        return await _context.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<object> GetUsageAsync(string key)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Key == key);
            
        if (apiKey == null) return new { error = "API key not found" };

        var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfMonth = currentMonth.AddDays(1 - currentMonth.Day);

        var monthlyUsage = await _context.ApiUsage
            .Where(u => u.ApiKeyId == apiKey.Id && u.Date >= startOfMonth)
            .CountAsync();

        var todayUsage = await _context.ApiUsage
            .Where(u => u.ApiKeyId == apiKey.Id && u.Date == currentMonth)
            .CountAsync();

        var last30Days = await _context.ApiUsage
            .Where(u => u.ApiKeyId == apiKey.Id && u.Date >= currentMonth.AddDays(-30))
            .GroupBy(u => u.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return new
        {
            apiKey = new
            {
                key = apiKey.Key,
                name = apiKey.Name,
                email = apiKey.Email,
                tier = apiKey.Tier,
                monthlyLimit = apiKey.MonthlyLimit,
                active = apiKey.Active,
                createdAt = apiKey.CreatedAt
            },
            usage = new
            {
                currentMonth = monthlyUsage,
                today = todayUsage,
                remaining = Math.Max(0, apiKey.MonthlyLimit - monthlyUsage),
                percentUsed = (double)monthlyUsage / apiKey.MonthlyLimit * 100,
                last30Days = last30Days
            },
            subscription = new
            {
                monthlyRevenue = apiKey.MonthlyRevenue,
                stripeCustomerId = apiKey.StripeCustomerId
            }
        };
    }

    public async Task<bool> UpgradeKeyAsync(string key, string newTier)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Key == key);
            
        if (apiKey == null) return false;

        var (monthlyLimit, revenue) = newTier switch
        {
            "pro" => (2000, 29.00m),
            "business" => (100000, 99.00m),
            "free" => (50, 0.00m),
            _ => throw new ArgumentException("Invalid tier")
        };

        apiKey.Tier = newTier;
        apiKey.MonthlyLimit = monthlyLimit;
        apiKey.MonthlyRevenue = revenue;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Upgraded API key {Key} to {Tier} tier", key, newTier);
        return true;
    }

    public async Task<Dictionary<string, object>> GetAnalyticsAsync()
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var monthStart = new DateOnly(now.Year, now.Month, 1);

        var totalKeys = await _context.ApiKeys.CountAsync();
        var activeKeys = await _context.ApiKeys.CountAsync(k => k.Active);
        var paidKeys = await _context.ApiKeys.CountAsync(k => k.Tier != "free" && k.Active);

        var monthlyRevenue = await _context.ApiKeys
            .Where(k => k.Active)
            .SumAsync(k => k.MonthlyRevenue);

        var todayRequests = await _context.ApiUsage
            .CountAsync(u => u.Date == today);

        var monthlyRequests = await _context.ApiUsage
            .CountAsync(u => u.Date >= monthStart);

        var recentRequests = await _context.ApiUsage
            .Where(u => u.Date >= today.AddDays(-7))
            .GroupBy(u => u.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var topEndpoints = await _context.ApiUsage
            .Where(u => u.Date >= monthStart)
            .GroupBy(u => u.Endpoint)
            .Select(g => new { Endpoint = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var errorRate = await _context.ApiUsage
            .Where(u => u.Date >= today.AddDays(-7))
            .GroupBy(u => u.Success)
            .Select(g => new { Success = g.Key, Count = g.Count() })
            .ToListAsync();

        return new Dictionary<string, object>
        {
            ["overview"] = new
            {
                totalApiKeys = totalKeys,
                activeApiKeys = activeKeys,
                paidCustomers = paidKeys,
                monthlyRevenue = monthlyRevenue,
                conversionRate = totalKeys > 0 ? (double)paidKeys / totalKeys * 100 : 0
            },
            ["requests"] = new
            {
                today = todayRequests,
                thisMonth = monthlyRequests,
                last7Days = recentRequests,
                avgResponseTime = await _context.ApiUsage
                    .Where(u => u.Date >= today.AddDays(-7) && u.Success)
                    .AverageAsync(u => (double)u.ResponseTimeMs)
            },
            ["endpoints"] = topEndpoints,
            ["errorRate"] = errorRate
        };
    }

    private static string GenerateApiKey(string tier)
    {
        var prefix = tier switch
        {
            "pro" => "hd_pro",
            "business" => "hd_biz", 
            _ => "hd_free"
        };

        var randomBytes = new byte[16];
        RandomNumberGenerator.Fill(randomBytes);
        var randomString = Convert.ToHexString(randomBytes).ToLower();

        return $"{prefix}_{randomString}";
    }
}