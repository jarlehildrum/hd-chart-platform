# HD Platform

Professional Human Design Chart API with commercial features.

## Architecture

- **Frontend:** Professional landing page with live demo
- **Backend:** .NET 10 Web API with authentication and rate limiting  
- **Chart Engine:** Calls internal HD Chart API (OpenClaw server)
- **Deployment:** Docker container on AH server (46.224.44.77)

## Features

✅ **API Key Authentication** - X-API-Key header validation
✅ **Rate Limiting** - Per-key monthly limits (Free: 50, Pro: 2000, Business: 100k)
✅ **Swagger/OpenAPI** - Auto-generated docs at `/docs`
✅ **Landing Page** - Professional dark theme with live demo
✅ **Self-service Signup** - Instant free API keys
✅ **Chart Types:** Natal, Transit, Composite, Image generation
✅ **Admin Endpoints** - Key management with admin secret

## Infrastructure

- **OpenClaw Server (91.98.95.203):** HD Chart API calculation engine (internal)
- **AH Server (46.224.44.77):** Production deployment target (public)
- **Docker:** Container deployment with nginx reverse proxy
- **Ports:** 8090 on AH server → 5000 in container

## Quick Start

### Local Development

```bash
cd src/HdPlatform
dotnet restore
dotnet run
# → http://localhost:5000
```

### Docker Build & Test

```bash
cd docker
docker-compose build
docker-compose up -d
# → http://localhost:8090
```

### Deploy to AH Server

```bash
# Copy to AH server
scp -r . root@46.224.44.77:/home/jarle/hd-platform/

# SSH to AH server
ssh root@46.224.44.77

# Deploy
cd /home/jarle/hd-platform/docker
export HD_ADMIN_SECRET="your_secret_here"
docker-compose up -d

# Setup nginx reverse proxy (if needed)
# Location: /etc/nginx/sites-available/hd-platform
```

## Environment Variables

- `HD_ADMIN_SECRET` - Admin API access (required)
- `HdChartApi__BaseUrl` - HD Chart API URL (default: http://100.101.12.75:5100)
- `DataDirectory` - API keys storage (default: /app/data)

## API Usage

### Get Free API Key

```bash
curl -X POST "https://your-domain.com/api/signup?name=TestUser&email=test@example.com"
```

### Calculate Chart

```bash
curl -X POST "https://your-domain.com/api/chart" \
  -H "X-API-Key: your_key_here" \
  -H "Content-Type: application/json" \
  -d '{"birthDate":"1990-03-15T14:30:00","birthPlace":"Oslo, Norway"}'
```

### Generate Bodygraph Image

```bash
curl -X POST "https://your-domain.com/api/chart/image" \
  -H "X-API-Key: your_key_here" \
  -H "Content-Type: application/json" \
  -d '{"birthDate":"1990-03-15T14:30:00","birthPlace":"Oslo, Norway"}' \
  -o bodygraph.png
```

## Business Model

- **Free:** 50 charts/month
- **Pro:** $29/month - 2,000 charts  
- **Business:** $79/month - 100,000 charts

## Security

- API keys stored in persistent Docker volume
- Admin endpoints protected by secret header
- Rate limiting prevents abuse
- HTTPS termination via nginx reverse proxy

---

**Next Steps:**
1. Deploy to AH server
2. Configure custom domain + SSL
3. Set up Stripe for Pro/Business tiers
4. Marketing launch