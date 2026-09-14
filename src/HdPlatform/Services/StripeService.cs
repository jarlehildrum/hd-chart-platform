using HdPlatform.Models;

namespace HdPlatform.Services;

// Simple stub for StripeService - payment integration disabled for now
public class StripeService
{
    private readonly ILogger<StripeService> _logger;

    public StripeService(ILogger<StripeService> logger)
    {
        _logger = logger;
    }

    public async Task<CheckoutResponse> CreateCheckoutSessionAsync(CheckoutRequest request)
    {
        _logger.LogInformation($"Payment disabled - CreateCheckoutSession called for price: {request.PriceId}");
        await Task.Delay(100); // Simulate async operation
        
        return new CheckoutResponse
        {
            CheckoutUrl = "/admin?payment=disabled",
            SessionId = "disabled"
        };
    }

    public async Task<BillingPortalResponse> CreateBillingPortalAsync(BillingPortalRequest request)
    {
        _logger.LogInformation($"Payment disabled - CreateBillingPortal called for API key: {request.ApiKey}");
        await Task.Delay(100);
        
        return new BillingPortalResponse
        {
            PortalUrl = "/admin"
        };
    }

    public async Task<bool> HandleStripeWebhookAsync(string payload, string signature)
    {
        _logger.LogInformation("Payment disabled - Stripe webhook received but ignored");
        await Task.Delay(100);
        return true;
    }

    public async Task<bool> HandleWebhookAsync(string payload, string signature)
    {
        _logger.LogInformation("Payment disabled - webhook received but ignored");
        await Task.Delay(100);
        return true;
    }

    public async Task<string> GetCustomerPortalUrlAsync(string customerEmail)
    {
        _logger.LogInformation($"Payment disabled - portal requested for: {customerEmail}");
        await Task.Delay(100);
        return "/admin"; // Redirect to admin instead
    }
}