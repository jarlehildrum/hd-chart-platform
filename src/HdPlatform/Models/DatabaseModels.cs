using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace HdPlatform.Models;

public class HdPlatformContext : DbContext
{
    public HdPlatformContext(DbContextOptions<HdPlatformContext> options) : base(options) { }

    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<ApiUsage> ApiUsage { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<ApiUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApiKeyId, e.Date });
            entity.HasOne<ApiKey>().WithMany().HasForeignKey(e => e.ApiKeyId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StripeCustomerId).IsUnique();
            entity.HasIndex(e => e.Email);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StripeSubscriptionId).IsUnique();
            entity.HasOne<Customer>().WithMany().HasForeignKey(e => e.CustomerId);
            entity.HasOne<ApiKey>().WithMany().HasForeignKey(e => e.ApiKeyId);
        });
    }
}

public class ApiKey
{
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string Tier { get; set; } = "free"; // free, pro, business
    
    public bool Active { get; set; } = true;
    
    public int MonthlyLimit { get; set; } = 50;
    
    public int CurrentMonthUsage { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? LastUsed { get; set; }
    
    // Stripe integration
    public string? StripeCustomerId { get; set; }
    
    public decimal MonthlyRevenue { get; set; } = 0;
}

public class ApiUsage
{
    public int Id { get; set; }
    
    public int ApiKeyId { get; set; }
    
    [Required, MaxLength(50)]
    public string Endpoint { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; }
    
    public DateOnly Date { get; set; }
    
    public int ResponseTimeMs { get; set; }
    
    public bool Success { get; set; }
    
    [MaxLength(255)]
    public string? ErrorMessage { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(255)]
    public string? UserAgent { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    
    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Name { get; set; }
    
    [Required, MaxLength(50)]
    public string StripeCustomerId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? LastLoginAt { get; set; }
    
    public decimal TotalRevenue { get; set; } = 0;
    
    public bool Active { get; set; } = true;
}

public class Subscription
{
    public int Id { get; set; }
    
    public int CustomerId { get; set; }
    
    public int ApiKeyId { get; set; }
    
    [Required, MaxLength(50)]
    public string StripeSubscriptionId { get; set; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string StripePriceId { get; set; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string Status { get; set; } = string.Empty; // active, canceled, past_due, etc.
    
    [Required, MaxLength(50)]
    public string PlanName { get; set; } = string.Empty; // pro, business
    
    public decimal MonthlyPrice { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? CanceledAt { get; set; }
    
    public DateTime CurrentPeriodStart { get; set; }
    
    public DateTime CurrentPeriodEnd { get; set; }
}

// Request/Response models for Stripe
public class CheckoutRequest
{
    [Required]
    public string PriceId { get; set; } = string.Empty;
    
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string ApiKey { get; set; } = string.Empty;
    
    public string? CustomerName { get; set; }
}

public class CheckoutResponse
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

public class BillingPortalRequest
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}

public class BillingPortalResponse
{
    public string PortalUrl { get; set; } = string.Empty;
}