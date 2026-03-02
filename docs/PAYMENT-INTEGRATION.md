# HD Platform - Payment Integration Plan

## Phase 1: Stripe Integration (Week 1-2)

### 1.1 Stripe Setup
- Create Stripe account
- Get API keys (test + live)
- Configure products:
  - **Free**: 50 requests/month (existing)
  - **Pro**: $29/month, 2000 requests/month  
  - **Business**: $99/month, 100k requests/month

### 1.2 Add Stripe to Platform
```bash
# Add Stripe package
dotnet add package Stripe.net

# Environment variables
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
```

### 1.3 Payment Endpoints
- `POST /api/checkout` - Create Stripe Checkout session
- `POST /api/webhooks/stripe` - Handle subscription events
- `GET /api/billing` - Customer billing portal

## Phase 2: Landing Page (Week 2)

### 2.1 Static Landing Page
- Pricing table with 3 tiers
- "Get Started" buttons → Stripe Checkout  
- Professional design (use template)
- Host on same container or Netlify

### 2.2 Key Features to Highlight
- Certified BG5 consultant built
- Swiss Ephemeris accuracy
- Professional bodygraph images  
- Developer-friendly REST API
- Swagger documentation

## Phase 3: Admin Dashboard (Week 3)

### 3.1 Database Schema
```sql
-- Extend existing API keys
ALTER TABLE api_keys ADD COLUMN:
- stripe_customer_id VARCHAR(255)
- stripe_subscription_id VARCHAR(255)  
- plan_name VARCHAR(50)
- mrr DECIMAL(10,2)
- last_payment DATE
- next_billing_date DATE
```

### 3.2 Retool Dashboard Views
- **Revenue**: MRR, churn, growth charts
- **Customers**: Active subscriptions, usage
- **API Keys**: All keys, limits, usage stats  
- **Failed Payments**: Dunning management

### 3.3 Key Metrics to Track
- Monthly Recurring Revenue (MRR)
- Customer Lifetime Value (CLV)
- API calls per customer
- Error rates by endpoint
- Geographic distribution

## Phase 4: Automation (Week 4)

### 4.1 Stripe Webhooks
- `customer.subscription.created` → Upgrade API key
- `customer.subscription.deleted` → Downgrade to free
- `invoice.payment_failed` → Send notification
- `customer.subscription.updated` → Update limits

### 4.2 Email Automation (SendGrid/Mailgun)
- Welcome email with API key
- Usage warnings (80%, 95%, 100%)
- Payment failure notifications
- Feature announcements

## Implementation Priority

### Minimum Viable Billing (Week 1):
1. ✅ Stripe Checkout integration  
2. ✅ Webhook for subscription.created
3. ✅ Auto-upgrade API key to "pro" tier
4. ✅ Customer billing portal link

### Enhanced Admin (Week 2-3):
1. Retool dashboard connected to database
2. Revenue tracking and MRR calculation  
3. Usage analytics and alerts
4. Customer support tools

### Growth Features (Month 2):
1. Annual plan discounts (2 months free)
2. Enterprise tier with custom limits
3. Affiliate/partner program  
4. Advanced usage analytics for customers

## Revenue Projections

**Conservative estimates:**
- Month 1: 5 customers = $145 MRR
- Month 3: 25 customers = $725 MRR  
- Month 6: 50 customers = $1,450 MRR
- Month 12: 100 customers = $2,900 MRR

**Target market:**
- BG5/HDL coaches (global community ~5,000)
- App developers needing HD data
- Astrology/spirituality platforms

## Technical Stack

```yaml
Payment: Stripe Billing + Checkout
Admin: Retool (recommended) or custom React
Analytics: Mixpanel or Amplitude  
Email: SendGrid or Mailgun
Hosting: Continue AH server
Database: PostgreSQL (upgrade from JSON files)
```

## Security Considerations

- Store Stripe customer IDs, not payment details
- Webhook signature validation required
- API key rotation on subscription changes
- PCI compliance through Stripe (not our concern)
- GDPR compliance for EU customers

## Launch Strategy

1. **Soft launch**: Announce in BG5 Facebook groups
2. **Content marketing**: Blog posts about API usage  
3. **Partner outreach**: Contact HD app developers
4. **Conference presence**: BG5 certification events

Target: $1000 MRR within 6 months (34 paying customers)