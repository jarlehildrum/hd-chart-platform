using Stripe;
using Stripe.Checkout;
using HdPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace HdPlatform.Services;

public class StripeService
{
    private readonly HdPlatformContext _context;
    private readonly ILogger<StripeService> _logger;
    private readonly IConfiguration _configuration;

    // Pricing configuration
    private static readonly Dictionary<string, (string PlanName, decimal Price, int MonthlyRequests)> StripePlans = new()
    {
        ["price_pro_hd_api"] = ("pro", 29.00m, 2000),
        ["price_business_hd_api"] = ("business", 99.00m, 100000)
    };

    public StripeService(HdPlatformContext context, ILogger<StripeService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;

        // Configure Stripe
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"] ?? 
            Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") ??
            throw new InvalidOperationException("Stripe secret key not configured");
    }

    public async Task<CheckoutResponse> CreateCheckoutSessionAsync(CheckoutRequest request)
    {
        try
        {
            // Validate API key exists
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Key == request.ApiKey && k.Active);
            
            if (apiKey == null)
                throw new ArgumentException("Invalid API key");

            // Validate price ID
            if (!StripePlans.TryGetValue(request.PriceId, out var plan))
                throw new ArgumentException("Invalid price ID");

            // Create or get Stripe customer
            var stripeCustomer = await GetOrCreateStripeCustomerAsync(apiKey.Email, request.CustomerName);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = request.PriceId,
                        Quantity = 1,
                    }
                },
                Mode = "subscription",
                SuccessUrl = _configuration["Stripe:SuccessUrl"] ?? "https://hdchartapi.com/success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _configuration["Stripe:CancelUrl"] ?? "https://hdchartapi.com/pricing",
                Customer = stripeCustomer.Id,
                Metadata = new Dictionary<string, string>
                {
                    ["api_key_id"] = apiKey.Id.ToString(),
                    ["plan_name"] = plan.PlanName,
                    ["email"] = apiKey.Email
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["api_key_id"] = apiKey.Id.ToString(),
                        ["plan_name"] = plan.PlanName
                    }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe checkout session {SessionId} for API key {ApiKey}", 
                session.Id, apiKey.Key);

            return new CheckoutResponse
            {
                CheckoutUrl = session.Url,
                SessionId = session.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe checkout session for {Email}", request.Email);
            throw;
        }
    }

    public async Task<BillingPortalResponse> CreateBillingPortalAsync(BillingPortalRequest request)
    {
        try
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Key == request.ApiKey && k.Active);
                
            if (apiKey == null)
                throw new ArgumentException("Invalid API key");

            if (string.IsNullOrEmpty(apiKey.StripeCustomerId))
                throw new ArgumentException("No billing information found");

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = apiKey.StripeCustomerId,
                ReturnUrl = _configuration["Stripe:ReturnUrl"] ?? "https://hdchartapi.com/account"
            };

            var service = new Stripe.BillingPortal.SessionService();
            var portalSession = await service.CreateAsync(options);

            return new BillingPortalResponse
            {
                PortalUrl = portalSession.Url
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating billing portal for API key {ApiKey}", request.ApiKey);
            throw;
        }
    }

    public async Task HandleStripeWebhookAsync(string payload, string signature)
    {
        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"] ?? 
                Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ??
                throw new InvalidOperationException("Stripe webhook secret not configured");

            var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);

            _logger.LogInformation("Processing Stripe webhook: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case Events.CustomerSubscriptionCreated:
                    await HandleSubscriptionCreatedAsync(stripeEvent);
                    break;
                    
                case Events.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdatedAsync(stripeEvent);
                    break;
                    
                case Events.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeletedAsync(stripeEvent);
                    break;
                    
                case Events.InvoicePaymentSucceeded:
                    await HandlePaymentSucceededAsync(stripeEvent);
                    break;
                    
                case Events.InvoicePaymentFailed:
                    await HandlePaymentFailedAsync(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            throw;
        }
    }

    private async Task<Stripe.Customer> GetOrCreateStripeCustomerAsync(string email, string? name = null)
    {
        var customerService = new CustomerService();
        
        // Try to find existing customer
        var existingCustomers = await customerService.ListAsync(new CustomerListOptions
        {
            Email = email,
            Limit = 1
        });

        if (existingCustomers.Data.Count > 0)
            return existingCustomers.Data[0];

        // Create new customer
        var customerOptions = new CustomerCreateOptions
        {
            Email = email,
            Name = name ?? email.Split('@')[0],
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "hd_platform"
            }
        };

        return await customerService.CreateAsync(customerOptions);
    }

    private async Task HandleSubscriptionCreatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription?.Metadata == null) return;

        if (!subscription.Metadata.TryGetValue("api_key_id", out var apiKeyIdStr) ||
            !int.TryParse(apiKeyIdStr, out var apiKeyId))
            return;

        var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey == null) return;

        var priceId = subscription.Items.Data[0].Price.Id;
        if (!StripePlans.TryGetValue(priceId, out var plan)) return;

        // Update API key
        apiKey.Tier = plan.PlanName;
        apiKey.MonthlyLimit = plan.MonthlyRequests;
        apiKey.StripeCustomerId = subscription.CustomerId;
        apiKey.MonthlyRevenue = plan.Price;

        // Create customer record
        var customer = new Customer
        {
            Email = apiKey.Email,
            Name = apiKey.Name,
            StripeCustomerId = subscription.CustomerId,
            CreatedAt = DateTime.UtcNow,
            TotalRevenue = plan.Price,
            Active = true
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Create subscription record
        var subscriptionRecord = new Subscription
        {
            CustomerId = customer.Id,
            ApiKeyId = apiKey.Id,
            StripeSubscriptionId = subscription.Id,
            StripePriceId = priceId,
            Status = subscription.Status,
            PlanName = plan.PlanName,
            MonthlyPrice = plan.Price,
            CreatedAt = DateTime.UtcNow,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd
        };

        _context.Subscriptions.Add(subscriptionRecord);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Upgraded API key {ApiKey} to {Plan} plan", apiKey.Key, plan.PlanName);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription?.Metadata == null) return;

        if (!subscription.Metadata.TryGetValue("api_key_id", out var apiKeyIdStr) ||
            !int.TryParse(apiKeyIdStr, out var apiKeyId))
            return;

        var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey == null) return;

        // Downgrade to free tier
        apiKey.Tier = "free";
        apiKey.MonthlyLimit = 50;
        apiKey.MonthlyRevenue = 0;

        // Update subscription status
        var subscriptionRecord = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscription.Id);
        
        if (subscriptionRecord != null)
        {
            subscriptionRecord.Status = "canceled";
            subscriptionRecord.CanceledAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Downgraded API key {ApiKey} to free tier", apiKey.Key);
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        var subscriptionRecord = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscription.Id);

        if (subscriptionRecord != null)
        {
            subscriptionRecord.Status = subscription.Status;
            subscriptionRecord.CurrentPeriodStart = subscription.CurrentPeriodStart;
            subscriptionRecord.CurrentPeriodEnd = subscription.CurrentPeriodEnd;
            await _context.SaveChangesAsync();
        }
    }

    private async Task HandlePaymentSucceededAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice?.SubscriptionId == null) return;

        var subscriptionRecord = await _context.Subscriptions
            .Include(s => s.CustomerId)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == invoice.SubscriptionId);

        if (subscriptionRecord != null)
        {
            var customer = await _context.Customers.FindAsync(subscriptionRecord.CustomerId);
            if (customer != null)
            {
                customer.TotalRevenue += (decimal)(invoice.AmountPaid ?? 0) / 100;
                await _context.SaveChangesAsync();
            }
        }

        _logger.LogInformation("Payment succeeded for subscription {SubscriptionId}, amount: {Amount}", 
            invoice.SubscriptionId, invoice.AmountPaid);
    }

    private async Task HandlePaymentFailedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice?.SubscriptionId == null) return;

        _logger.LogWarning("Payment failed for subscription {SubscriptionId}, amount: {Amount}", 
            invoice.SubscriptionId, invoice.AmountPaid);

        // TODO: Implement dunning management
        // - Send notification email
        // - Temporarily disable API key after X failed attempts
        // - Cancel subscription after Y days
    }
}