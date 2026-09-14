# 💳 Stripe Integration Guide

## 🎯 **Current Status: MANUAL TIER MANAGEMENT**

HD Platform has complete Stripe architecture prepared but currently uses manual tier management via admin interface.

---

## 🏗️ **Architecture Overview**

### **Payment Flow (When Activated):**
```
User → Frontend → /api/checkout → Stripe Checkout → Payment → Webhook → Tier Upgrade
```

### **Files Structure:**
```
src/HdPlatform/
├── Program.cs                     # Current: Manual tiers
├── Program-WithStripe.cs          # Ready: Full Stripe integration  
├── HdPlatform.csproj             # Current: No Stripe packages
├── HdPlatform-WithStripe.csproj  # Ready: With Stripe.net
├── Services/
│   └── StripeService.cs          # Current: Stub implementation
└── Models/
    └── DatabaseModels.cs         # Ready: Stripe models defined
```

---

## 💰 **Pricing Model**

### **Tier Structure:**
```bash
FREE:     $0/month     - 50 requests/month     - Self-signup
PRO:      $9.99/month  - 2,000 requests/month  - Stripe checkout  
BUSINESS: $99.99/month - 100,000 requests/month - Stripe checkout
```

### **Database Integration:**
```sql
-- ApiKeys table ready for Stripe
CREATE TABLE "ApiKeys" (
    "Id" SERIAL PRIMARY KEY,
    "Key" VARCHAR(100) UNIQUE NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(255) NOT NULL,
    "Tier" VARCHAR(50) DEFAULT 'free',
    "MonthlyLimit" INTEGER DEFAULT 50,
    "CurrentMonthUsage" INTEGER DEFAULT 0,
    "StripeCustomerId" VARCHAR(255),  -- Ready for Stripe
    "MonthlyRevenue" DECIMAL(10,2) DEFAULT 0,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "LastUsed" TIMESTAMP
);
```

---

## 🔧 **Stripe Activation Steps**

### **1. Stripe Account Setup**
```bash
# Register at dashboard.stripe.com
# Create products:
STRIPE_PRO_PRICE_ID=price_1xxxPro      # $9.99/month recurring
STRIPE_BUSINESS_PRICE_ID=price_1xxxBiz  # $99.99/month recurring

# Get API keys
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
```

### **2. Code Activation**
```bash
cd /home/jarle/hd-platform/src/HdPlatform

# Replace files with Stripe versions
mv Program-WithStripe.cs Program.cs
mv HdPlatform-WithStripe.csproj HdPlatform.csproj

# Update StripeService.cs with real implementation
```

### **3. Environment Configuration**
```bash
# Add to docker-compose.yml environment:
- STRIPE_SECRET_KEY=sk_test_...
- STRIPE_WEBHOOK_SECRET=whsec_...
- STRIPE_PRO_PRICE_ID=price_1xxxPro
- STRIPE_BUSINESS_PRICE_ID=price_1xxxBiz
```

### **4. Webhook Configuration**
```bash
# Add webhook endpoint in Stripe dashboard:
URL: https://yourdomain.com/api/webhooks/stripe
Events: customer.subscription.created, customer.subscription.updated, customer.subscription.deleted
```

---

## 🚀 **Stripe API Endpoints**

### **Checkout Session:**
```bash
POST /api/checkout
Headers: Content-Type: application/json
Body: {
  "priceId": "price_1xxxPro",
  "email": "customer@example.com", 
  "apiKey": "hd_free_xxxxx",
  "customerName": "Customer Name"
}

Response: {
  "checkoutUrl": "https://checkout.stripe.com/pay/cs_xxx",
  "sessionId": "cs_xxx"
}
```

### **Billing Portal:**
```bash
POST /api/billing-portal  
Headers: Content-Type: application/json
Body: {
  "apiKey": "hd_pro_xxxxx"
}

Response: {
  "portalUrl": "https://billing.stripe.com/p/session/xxx"
}
```

### **Webhook Handler:**
```bash
POST /api/webhooks/stripe
Headers: Stripe-Signature: t=xxx,v1=xxx
Body: {Stripe webhook payload}

# Handles: subscription events, payment updates, cancellations
```

---

## 💻 **Real StripeService Implementation**

### **Required Methods:**
```csharp
public class StripeService
{
    private readonly StripeClient _stripeClient;
    private readonly IConfiguration _config;
    private readonly DatabaseApiKeyService _keyService;

    // Create checkout session for plan upgrade
    public async Task<CheckoutResponse> CreateCheckoutSessionAsync(CheckoutRequest request)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = request.PriceId,
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            Customer = await GetOrCreateCustomerAsync(request.Email, request.CustomerName),
            SuccessUrl = $"{_baseUrl}/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_baseUrl}/cancel",
            Metadata = new Dictionary<string, string>
            {
                { "api_key", request.ApiKey },
                { "tier", GetTierFromPriceId(request.PriceId) }
            }
        };

        var service = new SessionService(_stripeClient);
        var session = await service.CreateAsync(options);
        
        return new CheckoutResponse
        {
            CheckoutUrl = session.Url,
            SessionId = session.Id
        };
    }

    // Handle subscription events from webhooks
    public async Task<bool> HandleStripeWebhookAsync(string payload, string signature)
    {
        var stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);
        
        switch (stripeEvent.Type)
        {
            case Events.CustomerSubscriptionCreated:
            case Events.CustomerSubscriptionUpdated:
                var subscription = stripeEvent.Data.Object as Subscription;
                await UpdateApiKeyFromSubscriptionAsync(subscription);
                break;
                
            case Events.CustomerSubscriptionDeleted:
                var deletedSub = stripeEvent.Data.Object as Subscription;
                await DowngradeApiKeyAsync(deletedSub);
                break;
        }
        
        return true;
    }
    
    // Create customer billing portal
    public async Task<BillingPortalResponse> CreateBillingPortalAsync(BillingPortalRequest request)
    {
        var apiKey = await _keyService.GetKeyAsync(request.ApiKey);
        
        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = apiKey.StripeCustomerId,
            ReturnUrl = $"{_baseUrl}/admin"
        };
        
        var service = new Stripe.BillingPortal.SessionService(_stripeClient);
        var session = await service.CreateAsync(options);
        
        return new BillingPortalResponse
        {
            PortalUrl = session.Url
        };
    }
}
```

---

## 🎨 **Frontend Integration**

### **Payment Button Example:**
```html
<button onclick="upgradeToProPlan('hd_free_xxxxx', 'user@example.com')">
  Upgrade to Pro - $9.99/month
</button>

<script>
async function upgradeToProPlan(apiKey, email) {
    const response = await fetch('/api/checkout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            priceId: 'price_1xxxPro',
            apiKey: apiKey,
            email: email,
            customerName: 'User Name'
        })
    });
    
    const result = await response.json();
    window.location.href = result.checkoutUrl;
}
</script>
```

### **Billing Portal Link:**
```html
<a href="#" onclick="openBillingPortal('hd_pro_xxxxx')">
  Manage Subscription
</a>

<script>
async function openBillingPortal(apiKey) {
    const response = await fetch('/api/billing-portal', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ apiKey: apiKey })
    });
    
    const result = await response.json();
    window.open(result.portalUrl);
}
</script>
```

---

## 📊 **Revenue Analytics**

### **Current Manual Tracking:**
```sql
-- Monthly revenue calculation (simulated)
SELECT 
  SUM(CASE 
    WHEN "Tier" = 'pro' THEN 9.99
    WHEN "Tier" = 'business' THEN 99.99
    ELSE 0
  END) as monthly_revenue
FROM "ApiKeys";
```

### **With Stripe (Real):**
```sql
-- Revenue tracking with Stripe integration
SELECT 
  ak."Tier",
  COUNT(*) as subscribers,
  SUM(ak."MonthlyRevenue") as revenue,
  AVG(ak."CurrentMonthUsage") as avg_usage
FROM "ApiKeys" ak
WHERE ak."StripeCustomerId" IS NOT NULL
GROUP BY ak."Tier";
```

---

## 🧪 **Testing Strategy**

### **Test Mode Setup:**
```bash
# Use Stripe test keys
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_test_...

# Test card numbers:
4242424242424242  # Visa success
4000000000000002  # Declined
4000000000009995  # Insufficient funds
```

### **Webhook Testing:**
```bash
# Install Stripe CLI
stripe listen --forward-to localhost:5000/api/webhooks/stripe

# Trigger test events
stripe trigger customer.subscription.created
stripe trigger customer.subscription.updated
```

---

## 🚦 **Production Checklist**

### **Before Stripe Activation:**
- [ ] Live Stripe account with business verification
- [ ] SSL certificate installed (required for webhooks)
- [ ] Production pricing products created in Stripe
- [ ] Webhook endpoints configured with production URL
- [ ] Error handling and logging implemented
- [ ] Tax configuration (if applicable)
- [ ] Terms of service and privacy policy
- [ ] Customer support contact information

### **Security Considerations:**
- [ ] Webhook signature validation
- [ ] Stripe API key security (environment variables)
- [ ] Customer data protection (GDPR/CCPA compliance)
- [ ] PCI DSS compliance (handled by Stripe)
- [ ] Rate limiting on payment endpoints
- [ ] Fraud prevention measures

---

## 🔄 **Migration Path**

### **From Manual to Stripe:**
1. **Current users:** Keep existing tier assignments
2. **New signups:** Continue with free tier self-signup
3. **Upgrades:** Route through Stripe checkout
4. **Existing paid:** Option to migrate to Stripe billing
5. **Grandfathering:** Honor existing manual agreements

### **Rollback Plan:**
- Keep manual admin tier management functional
- Stripe service can be disabled via environment flag
- Database schema supports both approaches
- Admin interface works independently of payment method

---

## 📈 **Business Benefits**

### **Automated Billing:**
- **Self-service upgrades:** Customers upgrade themselves
- **Recurring revenue:** Automatic subscription management
- **Failed payments:** Stripe handles retries and notifications
- **Compliance:** PCI DSS, tax calculations, international payments

### **Operational Efficiency:**
- **Reduced support:** Customers manage own billing
- **Scalability:** Handles growth without manual intervention  
- **Analytics:** Detailed payment and churn metrics
- **Integration:** Works with accounting systems

---

**💡 Current approach: Manual tier management provides full control while Stripe infrastructure is ready for activation when business scales.**