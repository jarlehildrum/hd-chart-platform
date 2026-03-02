# 🔮 HD Chart Platform

**Professional Human Design Chart API - Commercial SaaS Platform**

> Built by a certified BG5 consultant • Swiss Ephemeris accuracy • Production-ready • Revenue-generating

[![Status](https://img.shields.io/badge/Status-Production%20Ready-green.svg)](http://46.224.44.77:8090)
[![API](https://img.shields.io/badge/API-v1.0-blue.svg)](http://46.224.44.77:8090/docs)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)](./docker/Dockerfile)

---

## 🎯 **What is this?**

HD Chart Platform is a **complete commercial SaaS solution** for providing Human Design chart calculations via REST API. Built by a certified BG5 consultant with professional accuracy using Swiss Ephemeris.

**Live Demo:** http://46.224.44.77:8090

## ✨ **Key Features**

### 🔬 **Accurate Calculations**
- Swiss Ephemeris integration for precise planetary positions
- Certified BG5 consultant methodology  
- Automatic geocoding and timezone conversion
- All 64 gates, channels, and centers calculated

### 🎨 **Professional Bodygraphs**
- High-quality PNG image generation
- Personality & Design activation tables
- Gate numbers and line details visible
- Professional HD styling with proper fonts

### 💳 **Commercial Ready**
- 3-tier pricing model (Free/Pro/Business)
- API key authentication with rate limiting
- Self-service customer registration
- Ready for Stripe payment integration

### 🚀 **Production Deployment**
- Docker containerization with font support
- Health checks and monitoring
- Swagger API documentation
- JSON-based data persistence

---

## 📊 **API Endpoints**

### Public Endpoints
```http
GET  /api                 # API status
GET  /api/health          # Health check  
POST /api/signup          # Get free API key
POST /api/demo/chart      # Demo chart calculation
POST /api/demo/image      # Demo bodygraph image
```

### Authenticated Endpoints (Requires API Key)
```http
POST /api/chart           # Full chart calculation
POST /api/chart/utc       # UTC-based chart
POST /api/chart/transit   # Transit calculations  
POST /api/chart/composite # Composite charts
POST /api/chart/image     # Professional bodygraph PNG
```

### Admin Endpoints (Requires Admin Secret)
```http
GET  /api/admin/keys      # List all API keys
POST /api/admin/keys      # Create API key
GET  /api/admin/usage/{key} # Usage statistics
```

---

## 🏗️ **Architecture**

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Landing Page  │───▶│   HD Platform    │───▶│ Swiss Ephemeris │
│   (Pricing)     │    │   (.NET API)     │    │   (Astrology)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌──────────────────┐
                       │   Image Service  │
                       │   (SkiaSharp)    │
                       └──────────────────┘
```

**Tech Stack:**
- **.NET 10** - Modern C# Web API
- **Swiss Ephemeris** - Astronomical calculations
- **SkiaSharp** - Professional image generation  
- **Docker** - Containerized deployment
- **JSON** - Lightweight data storage

---

## 🚀 **Quick Start**

### Prerequisites
- Docker installed
- .NET 10 SDK (for development)

### Run with Docker
```bash
# Clone repository
git clone https://github.com/yourusername/hd-chart-platform.git
cd hd-chart-platform

# Build and run
docker build -f docker/Dockerfile -t hd-platform .
docker run -d -p 8090:5000 \
  -e HD_ADMIN_SECRET="your_admin_secret_here" \
  -v $(pwd)/data:/app/data \
  hd-platform

# Access API
curl http://localhost:8090/api
```

### Development Setup
```bash
cd src/HdPlatform
dotnet restore
dotnet run

# API available at http://localhost:5000
# Swagger docs at http://localhost:5000/docs
```

---

## 💰 **Business Model**

### Pricing Tiers
| Tier | Price | Monthly Requests | Target Market |
|------|-------|------------------|---------------|
| **Free** | $0 | 50 | Testing & small projects |
| **Pro** | $29 | 2,000 | BG5 coaches & app developers |
| **Business** | $99 | 100,000 | Platforms & enterprises |

### Revenue Potential
- **Target Market:** Global BG5/HDL community (~5,000 coaches)
- **Conservative Estimate:** 100 paying customers = $2,900 MRR
- **Growth Potential:** Enterprise clients, white-label solutions

---

## 🛠️ **Deployment**

### Production Server
Currently deployed on **AH Server (46.224.44.77:8090)**

```bash
# Deploy to production
docker build -f docker/Dockerfile -t hd-platform .
docker run -d --name hd-platform \
  -p 8090:5000 \
  -e HD_ADMIN_SECRET="hd_admin_536290824388ea22" \
  -v /home/jarle/hd-platform-data:/app/data \
  hd-platform
```

### Environment Variables
```bash
HD_ADMIN_SECRET=your_admin_secret
STRIPE_SECRET_KEY=sk_live_... # (when implemented)
STRIPE_WEBHOOK_SECRET=whsec_... # (when implemented)  
```

---

## 📈 **Next Steps**

### Phase 1: Payment Integration (Week 1)
- [ ] Set up Stripe account
- [ ] Implement subscription billing
- [ ] Auto-upgrade API keys on payment
- [ ] Customer billing portal

### Phase 2: Admin Dashboard (Week 2)  
- [ ] Retool dashboard for analytics
- [ ] Customer usage tracking
- [ ] Revenue monitoring
- [ ] Support tools

### Phase 3: Marketing & Growth (Week 3-4)
- [ ] Professional landing page
- [ ] BG5 community outreach  
- [ ] Developer documentation
- [ ] Content marketing

---

## 📁 **Project Structure**

```
hd-chart-platform/
├── src/
│   ├── HdPlatform/          # Main API source
│   │   ├── Program.cs       # API endpoints & config
│   │   ├── Services/        # Business logic
│   │   ├── Models/          # Data structures
│   │   └── Middleware/      # Authentication & rate limiting
│   └── HdChartApi/          # Integrated chart calculation engine
├── docker/
│   └── Dockerfile          # Production container
├── docs/
│   └── PAYMENT-INTEGRATION.md # Stripe setup guide
├── ephe/                   # Swiss Ephemeris data files
├── wwwroot/               # Static web assets
└── README.md              # This file
```

---

## 🏆 **Built With Expertise**

- **Certified BG5 Consultant** - Professional Human Design knowledge
- **Swiss Ephemeris** - Same engine used by professional astrology software
- **Production-Ready Code** - Built for scale and reliability
- **Commercial License Ready** - Prepared for CHF 750 Swiss Ephemeris license

---

## 📞 **Support & Contact**

- **Documentation:** See `/docs` folder
- **API Documentation:** http://46.224.44.77:8090/docs
- **Issues:** Use GitHub Issues tab
- **Business Inquiries:** Contact repository owner

---

## 📄 **License**

Private commercial project. All rights reserved.

**Note:** Swiss Ephemeris requires commercial license (CHF 750) for commercial use beyond AGPL terms.

---

<div align="center">

**🚀 Ready to revolutionize Human Design technology**

*Professional • Accurate • Scalable • Commercial*

</div>