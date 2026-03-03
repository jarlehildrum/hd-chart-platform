# 🌟 HD Platform - Human Design Chart API

Professional Human Design chart calculation API with PostgreSQL storage, admin interface, and analytics. Built by a certified BG5 consultant.

## 🚀 **Features**

### ✅ **Core API**
- **HD Chart calculations** with Swiss Ephemeris
- **Geocoding & timezone** conversion
- **Transit & composite** charts  
- **Professional PNG images** generation
- **PostgreSQL database** with usage tracking
- **API key management** with tier limits
- **Rate limiting** and authentication

### ✅ **Admin Interface**
- **Web dashboard** at `/admin`
- **API key creation** and management
- **Live statistics** and usage tracking
- **Grafana analytics** integration
- **PostgreSQL admin** via direct queries

### ✅ **Analytics**
- **Grafana dashboards** on port 3001
- **Real-time metrics** (API usage, success rates, revenue)
- **PostgreSQL data source** integration
- **Custom HD Platform** metrics

### 🔄 **Stripe Integration (Prepared)**
- **Payment architecture** ready but deactivated
- **Manual tier management** via admin
- **Revenue tracking** functional
- **Ready for production** billing when needed

---

## 🎯 **Quick Start**

### **Deploy on AH Server:**
```bash
# 1. Clone to AH server
git clone <repo> /home/jarle/hd-platform
cd /home/jarle/hd-platform

# 2. Start all services
docker-compose up -d

# 3. Access platform
# API:        http://46.224.44.77/api
# Admin:      http://46.224.44.77/admin 
# Analytics:  http://46.224.44.77:3001
# Docs:       http://46.224.44.77/docs
```

### **Admin Access:**
- **URL:** http://46.224.44.77/admin
- **Secret:** `${HD_ADMIN_SECRET}` (literal string)
- **Features:** Key management, statistics, analytics link

### **Grafana Setup:**
1. **Go to:** http://46.224.44.77:3001
2. **Login:** admin / hdplatform123
3. **Data Source:** PostgreSQL (`postgres:5432`, `hdplatform`, `hduser:hdplatform123`)
4. **Import:** `/workspace/hd-dashboard-working.json`

---

## 🏗️ **Architecture**

### **Services (Docker Compose):**
```
┌─────────────────┬──────────────┬─────────────────┐
│ Service         │ Port         │ Purpose         │
├─────────────────┼──────────────┼─────────────────┤
│ hd-platform-api │ Internal     │ API endpoints   │
│ hd-platform-web │ Internal     │ Static files    │
│ hd-postgres     │ Internal     │ Database        │
│ hd-redis        │ Internal     │ Rate limiting   │
│ hd-grafana      │ 3001         │ Analytics       │
│ hd-nginx        │ 80           │ Reverse proxy   │
└─────────────────┴──────────────┴─────────────────┘
```

### **Database Schema:**
```sql
-- API Keys with tier management
ApiKeys: Id, Key, Name, Email, Tier, Active, MonthlyLimit, 
         CurrentMonthUsage, CreatedAt, LastUsed, StripeCustomerId, MonthlyRevenue

-- Usage tracking for analytics  
ApiUsage: Id, ApiKeyId, Endpoint, Timestamp, Date, ResponseTimeMs, 
          Success, ErrorMessage, IpAddress, UserAgent
```

### **API Tiers:**
```
FREE:     $0/month    - 50 requests/month
PRO:      $9.99/month - 2,000 requests/month  
BUSINESS: $99.99/month - 100,000 requests/month
```

---

## 📊 **API Endpoints**

### **Public:**
```bash
GET  /api              # API info
GET  /api/health       # Health check
POST /api/signup       # Get free API key
```

### **Protected (require X-API-Key):**
```bash
POST /api/chart        # Calculate natal chart
POST /api/chart/utc    # UTC-based chart
POST /api/chart/transit # Transit overlay
POST /api/chart/composite # Relationship chart
POST /api/chart/image  # Generate PNG
```

### **Admin (require X-Admin-Secret):**
```bash
GET  /api/admin/keys   # List all API keys
POST /api/admin/keys   # Create new API key
GET  /api/admin/usage/{key} # Usage statistics
GET  /api/admin/analytics   # Platform analytics
```

### **Demo (no auth):**
```bash
POST /api/demo/chart   # Demo chart calculation
POST /api/demo/image   # Demo image generation  
```

---

## 💰 **Stripe Integration**

### **Current Status: 🔴 MANUAL**
- **Payment processing:** Disabled
- **Tier management:** Manual via admin
- **Revenue tracking:** Simulated based on tier
- **Architecture:** Ready for activation

### **Files Ready for Stripe:**
```
src/HdPlatform/Program-WithStripe.cs          # Full Stripe endpoints
src/HdPlatform/HdPlatform-WithStripe.csproj   # With Stripe.net package  
src/HdPlatform/Services/StripeService.cs      # Needs real implementation
```

### **Stripe Endpoints (when activated):**
```bash
POST /api/checkout          # Create checkout session
POST /api/billing-portal    # Customer portal  
POST /api/webhooks/stripe   # Payment webhooks
```

### **To Activate Stripe:**
1. **Stripe Account:** Get API keys from dashboard.stripe.com
2. **Products:** Create Pro ($9.99) and Business ($99.99) plans
3. **Code:** Replace Program.cs with Program-WithStripe.cs
4. **Environment:** Set STRIPE_SECRET_KEY, STRIPE_WEBHOOK_SECRET
5. **Deploy:** Update containers with Stripe environment

---

## 🔧 **Development**

### **Local Development:**
```bash
# Prerequisites: .NET 8, PostgreSQL, Redis
cd src/HdPlatform
dotnet run

# Database migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### **Docker Development:**
```bash
# Build custom image
docker build -f docker/Dockerfile -t hd-platform .

# Run with docker-compose
docker-compose up -d --build
```

### **Environment Variables:**
```bash
# Required
DATABASE_URL=Host=postgres;Port=5432;Database=hdplatform;Username=hduser;Password=hdplatform123
HD_ADMIN_SECRET=${HD_ADMIN_SECRET}

# Optional (for Stripe activation)
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
```

---

## 📈 **Analytics & Monitoring**

### **Grafana Dashboards:**
- **API Usage:** Request volume over time
- **Success Rates:** Error monitoring  
- **Revenue Tracking:** Monthly earnings
- **User Overview:** API key usage statistics

### **Database Queries:**
```sql
-- Current platform stats
SELECT 
  (SELECT COUNT(*) FROM "ApiKeys") as total_keys,
  (SELECT COUNT(*) FROM "ApiUsage" WHERE "Timestamp" >= NOW() - INTERVAL '24 hours') as daily_requests,
  (SELECT SUM("MonthlyRevenue") FROM "ApiKeys") as monthly_revenue;
```

### **Performance Monitoring:**
- **Response times:** Tracked per endpoint
- **Rate limiting:** Redis-based with PostgreSQL logging
- **Error tracking:** All failures logged with context
- **Uptime:** Docker health checks and restart policies

---

## 🚦 **Production Deployment**

### **Current Deployment:**
- **Server:** AH-server (46.224.44.77)
- **Ports:** 80 (nginx), 3001 (grafana)
- **SSL:** Ready for Let's Encrypt
- **Backups:** PostgreSQL volume persistence

### **Scaling Considerations:**
- **Load balancing:** nginx ready for multiple API instances
- **Database:** PostgreSQL with connection pooling
- **Caching:** Redis for rate limiting, expandable for chart caching
- **CDN:** Static files served via nginx

### **Security:**
- **API keys:** Secure generation with tier validation
- **Rate limiting:** Per-key and global limits
- **Admin access:** Secret-based authentication
- **CORS:** Configured for web app integration

---

## 📚 **Documentation**

### **API Documentation:**
- **Swagger UI:** http://46.224.44.77/docs
- **OpenAPI Spec:** Auto-generated from code
- **Authentication:** X-API-Key header examples

### **Chart Examples:**
```json
// Natal chart request
{
  "birthDate": "1968-11-23T21:19:00", 
  "birthPlace": "Trondheim, Norway"
}

// Response includes: type, profile, authority, strategy, centers, gates, channels
```

### **Admin Examples:**
```bash
# Create business tier API key
curl -X POST "http://46.224.44.77/admin/keys?name=ClientName&email=client@example.com&tier=business" \
  -H "X-Admin-Secret: ${HD_ADMIN_SECRET}"
```

---

## 🏆 **Success Metrics**

### **Performance (Stress Tested):**
- **Peak:** 828 requests/second
- **Success Rate:** 100% under load
- **Avg Response:** 85-120ms
- **Concurrent Users:** Tested with 50+ simultaneous

### **Current Data (Live):**
- **API Keys:** 2 active
- **24h Requests:** 16 
- **Success Rate:** 87.5%
- **Revenue:** $99.00/month

---

## 🛠️ **Maintenance**

### **Regular Tasks:**
```bash
# Check system health
docker-compose ps
curl http://46.224.44.77/health

# Database maintenance
docker exec hd-postgres psql -U hduser -d hdplatform -c "SELECT COUNT(*) FROM \"ApiUsage\";"

# View logs
docker-compose logs -f hd-platform-api
docker-compose logs -f hd-grafana
```

### **Backup Strategy:**
```bash
# Database backup
docker exec hd-postgres pg_dump -U hduser hdplatform > backup-$(date +%Y%m%d).sql

# Full system backup  
tar -czf hd-platform-backup-$(date +%Y%m%d).tar.gz /home/jarle/hd-platform/
```

---

## 🎯 **Next Steps**

### **Immediate (Optional):**
- [ ] SSL/HTTPS with Let's Encrypt
- [ ] Custom domain setup
- [ ] Enhanced Grafana dashboards
- [ ] Automated backups

### **Business Growth:**
- [ ] Activate Stripe billing
- [ ] Frontend web app
- [ ] API documentation site  
- [ ] Customer onboarding flow

### **Technical Scaling:**
- [ ] Chart result caching
- [ ] Multiple server deployment
- [ ] API versioning
- [ ] Enhanced monitoring

---

**🌟 HD Platform is production-ready with manual tier management!**

Built with ❤️ by a certified BG5 consultant using .NET 8, PostgreSQL, Redis, Grafana, and Swiss Ephemeris.