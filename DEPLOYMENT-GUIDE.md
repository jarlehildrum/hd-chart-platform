# 🚀 HD Platform - Deployment Guide

**Complete deployment guide for HD Platform with Stripe billing and Grafana analytics**

## 📋 **Prerequisites**

- Docker & Docker Compose installed
- Stripe account with API keys  
- Domain name (optional, for SSL)
- 2GB+ RAM, 20GB+ storage

## ⚡ **Quick Start (5 minutes)**

### 1. Clone and Configure
```bash
git clone https://github.com/jarlehildrum/hd-chart-platform.git
cd hd-chart-platform

# Copy environment template
cp .env.example .env

# Edit .env with your values
nano .env
```

### 2. Required Environment Variables
```bash
# Stripe (REQUIRED for billing)
STRIPE_SECRET_KEY=sk_test_your_key_here
STRIPE_WEBHOOK_SECRET=whsec_your_secret_here

# Database passwords
POSTGRES_PASSWORD=your_secure_password
GF_SECURITY_ADMIN_PASSWORD=your_grafana_password

# Admin secret
HD_ADMIN_SECRET=your_super_secure_admin_secret
```

### 3. Start Platform
```bash
# Start all services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f hd-platform
```

### 4. Access Services
- **HD Platform API:** http://localhost:8090
- **API Documentation:** http://localhost:8090/docs  
- **Grafana Analytics:** http://localhost:3000 (admin/hdplatform123)
- **PostgreSQL:** localhost:5432

## 🔧 **Stripe Setup**

### 1. Create Stripe Account
1. Sign up at https://stripe.com
2. Verify your business details
3. Get API keys from Dashboard → Developers → API keys

### 2. Create Products
Create these products in Stripe Dashboard:

```
Pro Plan
- Price ID: price_pro_hd_api
- Amount: $29.00/month
- Recurring: Monthly

Business Plan  
- Price ID: price_business_hd_api
- Amount: $99.00/month
- Recurring: Monthly
```

### 3. Setup Webhook
1. Go to Stripe Dashboard → Developers → Webhooks
2. Add endpoint: `https://yourdomain.com/api/webhooks/stripe`
3. Select events:
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.payment_succeeded`
   - `invoice.payment_failed`
4. Copy webhook secret to `.env`

## 📊 **Grafana Dashboards**

### Default Dashboard: HD Platform Overview
- **Monthly Recurring Revenue (MRR)**
- **Customer metrics** (total, paying, conversion rate)
- **API usage trends**
- **Endpoint performance**
- **Revenue growth charts**

### Access Grafana
1. Navigate to http://localhost:3000
2. Login: admin / hdplatform123
3. Dashboard: "HD Platform - Business Overview"

### Custom Dashboards
All dashboards are pre-configured and connected to PostgreSQL data source.

## 🌐 **Production Deployment**

### Domain & SSL Setup
```bash
# 1. Point domain to your server
# 2. Update .env with domain
DOMAIN_NAME=hdchartapi.com
CERTBOT_EMAIL=admin@hdchartapi.com

# 3. Enable SSL (uncomment nginx in docker-compose.yml)
docker-compose up -d nginx
```

### Performance Optimization
```bash
# Increase PostgreSQL performance
echo "shared_preload_libraries = 'pg_stat_statements'" >> /etc/postgresql/postgresql.conf

# Enable connection pooling
docker-compose exec postgres psql -U hduser -d hdplatform -c "CREATE EXTENSION IF NOT EXISTS pg_stat_statements;"
```

### Backup Strategy
```bash
# Database backup script
docker exec hd-postgres pg_dump -U hduser hdplatform > backup_$(date +%Y%m%d_%H%M%S).sql

# Set up automated backups (cron)
0 2 * * * /path/to/backup-script.sh
```

## 🔍 **Monitoring & Logs**

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f hd-platform
docker-compose logs -f postgres
docker-compose logs -f grafana
```

### Health Checks
```bash
# API health
curl http://localhost:8090/api/health

# Database health  
docker-compose exec postgres pg_isready -U hduser

# Grafana health
curl http://localhost:3000/api/health
```

### Performance Metrics
- **Response time:** Tracked in Grafana
- **Error rate:** Available in analytics endpoint
- **Database performance:** PostgreSQL logs
- **Revenue metrics:** Real-time in Grafana

## 🧪 **Testing the Platform**

### 1. Test API
```bash
# Get free API key
curl -X POST "http://localhost:8090/api/signup?name=Test&email=test@example.com"

# Test chart calculation
curl -X POST "http://localhost:8090/api/demo/chart" \
  -H "Content-Type: application/json" \
  -d '{"birthDate":"1968-11-23T21:19:00","birthPlace":"Trondheim, Norway"}'
```

### 2. Test Billing
```bash
# Create checkout session
curl -X POST "http://localhost:8090/api/checkout" \
  -H "Content-Type: application/json" \
  -d '{"priceId":"price_pro_hd_api","email":"customer@example.com","apiKey":"your_api_key"}'
```

### 3. Test Analytics
- Visit Grafana dashboard
- Check revenue metrics
- Monitor API usage

## 🚨 **Troubleshooting**

### Common Issues

**Database connection failed:**
```bash
# Check PostgreSQL is running
docker-compose ps postgres

# Check logs
docker-compose logs postgres

# Reset database
docker-compose down -v
docker-compose up -d postgres
```

**Stripe webhook not working:**
```bash
# Check webhook secret in .env
echo $STRIPE_WEBHOOK_SECRET

# Test webhook endpoint
curl -X POST http://localhost:8090/api/webhooks/stripe

# Check Stripe dashboard for delivery attempts
```

**Grafana dashboard empty:**
```bash
# Check database connection
docker-compose exec grafana grafana-cli admin reset-admin-password hdplatform123

# Restart Grafana
docker-compose restart grafana
```

### Debug Mode
```bash
# Enable debug logging
echo "LOG_LEVEL=Debug" >> .env
docker-compose restart hd-platform
```

## 📈 **Scaling & Optimization**

### Horizontal Scaling
```bash
# Scale API instances
docker-compose up -d --scale hd-platform=3

# Add load balancer (uncomment nginx in docker-compose.yml)
docker-compose up -d nginx
```

### Database Optimization
```bash
# Connection pooling
echo "max_connections = 200" >> postgresql.conf

# Performance tuning
echo "shared_buffers = 256MB" >> postgresql.conf
echo "effective_cache_size = 1GB" >> postgresql.conf
```

### Caching Layer
```bash
# Enable Redis caching
docker-compose up -d redis

# Configure API to use Redis (update appsettings.json)
```

## 💰 **Revenue Optimization**

### Monitor Key Metrics
- **MRR Growth Rate:** Target 20% monthly
- **Conversion Rate:** Target 3-5%
- **Churn Rate:** Keep below 5%
- **API Usage:** Monitor trends

### Upgrade Strategies
- **Usage-based upgrades:** Auto-suggest when approaching limits
- **Feature upsells:** Advanced analytics, white-labeling
- **Enterprise deals:** Custom pricing for high-volume

## 🔐 **Security Checklist**

- [ ] Change all default passwords
- [ ] Enable SSL/TLS certificates  
- [ ] Set up firewall rules
- [ ] Regular security updates
- [ ] Backup encryption
- [ ] API rate limiting configured
- [ ] Webhook signature validation
- [ ] Environment variables secured

## 🎯 **Next Steps**

1. **Week 1:** Deploy and test Stripe integration
2. **Week 2:** Set up monitoring and alerts  
3. **Week 3:** Launch marketing and customer acquisition
4. **Week 4:** Optimize based on real usage data

## 📞 **Support**

- **Documentation:** See `/docs` folder
- **Issues:** GitHub Issues
- **Business:** Contact repository owner
- **Emergency:** Check Grafana alerts

---

**🚀 Your professional HD Chart Platform with billing and analytics is ready!**