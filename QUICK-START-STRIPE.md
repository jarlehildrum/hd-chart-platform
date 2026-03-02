# Quick Start: Stripe Integration for HD Platform

## 1. Setup Stripe Account (10 min)

1. Go to https://stripe.com → Create account
2. Get API keys from Dashboard → Developers → API keys
3. Note down:
   - `Publishable key` (pk_test_...)  
   - `Secret key` (sk_test_...)

## 2. Create Products in Stripe (5 min)

```bash
# Products to create in Stripe Dashboard:
1. HD API Pro - $29/month - 2000 requests
2. HD API Business - $99/month - 100k requests
```

## 3. Add to HD Platform (30 min)

```bash
cd /home/jarle/.openclaw3/workspace/hd-platform/src/HdPlatform
dotnet add package Stripe.net
```

Add environment variables to docker-compose:
```yaml
environment:
  - STRIPE_SECRET_KEY=sk_test_your_key
  - STRIPE_WEBHOOK_SECRET=whsec_your_secret
```

## 4. Simple Checkout Endpoint (20 min)

Add to Program.cs:
```csharp
// Checkout endpoint
app.MapPost("/api/checkout", (CheckoutRequest request) =>
{
    StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
    
    var options = new SessionCreateOptions
    {
        PaymentMethodTypes = new List<string> { "card" },
        LineItems = new List<SessionLineItemOptions>
        {
            new()
            {
                Price = request.PriceId, // from Stripe Dashboard
                Quantity = 1,
            },
        },
        Mode = "subscription",
        SuccessUrl = "https://yourdomain.com/success",
        CancelUrl = "https://yourdomain.com/cancel",
        CustomerEmail = request.Email,
    };
    
    var service = new SessionService();
    var session = service.Create(options);
    
    return Results.Ok(new { checkoutUrl = session.Url });
});

// Webhook endpoint  
app.MapPost("/api/webhooks/stripe", async (HttpContext context) =>
{
    var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var stripeEvent = EventUtility.ConstructEvent(
        json,
        context.Request.Headers["Stripe-Signature"], 
        Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
    );

    if (stripeEvent.Type == Events.CustomerSubscriptionCreated)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        // TODO: Upgrade customer's API key to pro tier
        // Find API key by customer email and update tier
    }
    
    return Results.Ok();
});
```

## 5. Simple Landing Page (60 min)

Create `/wwwroot/pricing.html`:
```html
<!DOCTYPE html>
<html>
<head>
    <title>HD Chart API - Pricing</title>
    <script src="https://js.stripe.com/v3/"></script>
</head>
<body>
    <h1>Professional Human Design Chart API</h1>
    
    <div class="pricing-table">
        <div class="plan">
            <h3>Free</h3>
            <p>50 requests/month</p>
            <p>Perfect for testing</p>
            <a href="/api/signup">Get Started</a>
        </div>
        
        <div class="plan featured">
            <h3>Pro</h3>
            <p>$29/month</p>
            <p>2,000 requests/month</p>
            <p>Ideal for apps & coaches</p>
            <button onclick="checkout('price_pro_id')">Subscribe</button>
        </div>
        
        <div class="plan">
            <h3>Business</h3>
            <p>$99/month</p>
            <p>100,000 requests/month</p>
            <p>For platforms & enterprises</p>
            <button onclick="checkout('price_business_id')">Subscribe</button>
        </div>
    </div>

    <script>
    const stripe = Stripe('pk_test_your_publishable_key');
    
    async function checkout(priceId) {
        const response = await fetch('/api/checkout', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                priceId: priceId,
                email: prompt('Enter your email:')
            })
        });
        
        const { checkoutUrl } = await response.json();
        window.location = checkoutUrl;
    }
    </script>
</body>
</html>
```

## 6. Quick Admin with Retool (30 min)

1. Create free Retool account
2. Connect to your database (will need PostgreSQL instead of JSON files)
3. Build tables showing:
   - Customer list with MRR
   - API usage by customer  
   - Revenue charts
   - Failed payments

## 7. Database Upgrade (optional but recommended)

```bash
# Move from JSON files to PostgreSQL
docker run -d \
  --name hd-postgres \
  -e POSTGRES_DB=hdplatform \
  -e POSTGRES_USER=hduser \
  -e POSTGRES_PASSWORD=secure123 \
  -p 5432:5432 \
  postgres:15

# Update HdPlatform to use EF Core + PostgreSQL
```

## Result After 2-3 Hours:

✅ Working Stripe integration  
✅ Subscription plans ($29 & $99)
✅ Auto-upgrade API keys on payment
✅ Basic admin dashboard
✅ Professional pricing page

**Estimated revenue impact:**
- Week 1: First paying customer
- Month 1: 5-10 customers = $150-500 MRR  
- Month 3: 25+ customers = $750+ MRR