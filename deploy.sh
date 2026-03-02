#!/bin/bash
set -e

echo "🚀 Deploying HD Platform to AH Server"

# Configuration
AH_SERVER="root@46.224.44.77"
DEPLOY_PATH="/home/jarle/hd-platform"
ADMIN_SECRET=${HD_ADMIN_SECRET:-"hd_admin_$(openssl rand -hex 8)"}

echo "📦 Building Docker image..."
cd docker
docker build -t hd-platform:latest -f Dockerfile ..

echo "💾 Saving Docker image..."
docker save hd-platform:latest | gzip > hd-platform.tar.gz

echo "📤 Uploading to AH server..."
rsync -av --delete ../. $AH_SERVER:$DEPLOY_PATH/
scp hd-platform.tar.gz $AH_SERVER:$DEPLOY_PATH/

echo "🔧 Deploying on AH server..."
ssh $AH_SERVER << EOF
cd $DEPLOY_PATH
echo "Loading Docker image..."
docker load < hd-platform.tar.gz
rm hd-platform.tar.gz

echo "Setting environment variables..."
export HD_ADMIN_SECRET="$ADMIN_SECRET"

echo "Starting containers..."
cd docker
docker-compose down --remove-orphans 2>/dev/null || true
docker-compose up -d

echo "Waiting for service..."
sleep 10

echo "Testing deployment..."
curl -f http://localhost:8080/api || echo "⚠️  Service not responding yet"

echo "✅ Deployment complete!"
echo "🌐 Service available on: http://46.224.44.77:8080"
echo "📚 API docs: http://46.224.44.77:8080/docs"
echo "🔑 Admin secret: $ADMIN_SECRET"
EOF

echo "🎉 HD Platform deployed successfully!"
echo ""
echo "Next steps:"
echo "1. Test: http://46.224.44.77:8080"
echo "2. Setup nginx reverse proxy (optional)"
echo "3. Configure custom domain + SSL"