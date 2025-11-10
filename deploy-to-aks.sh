#!/bin/bash

# AKS Deployment Script for Payment API
# This script creates an AKS cluster and deploys the payment service with NGINX Ingress

set -e

# Variables
RESOURCE_GROUP="Dev-Demo-App-Rg"
AKS_NAME="payment-aks-cluster"
LOCATION="centralindia"
NODE_COUNT=2
NODE_SIZE="Standard_B2s"  # Free tier eligible
ACR_NAME="demoimagecontainer"
DNS_NAME="payment-api"

echo "=========================================="
echo "AKS Deployment for Payment API"
echo "=========================================="

# Step 1: Create AKS Cluster
echo ""
echo "Step 1: Creating AKS cluster..."
az aks create \
  --resource-group $RESOURCE_GROUP \
  --name $AKS_NAME \
  --location $LOCATION \
  --node-count $NODE_COUNT \
  --node-vm-size $NODE_SIZE \
  --enable-managed-identity \
  --generate-ssh-keys \
  --network-plugin azure \
  --enable-addons monitoring \
  --attach-acr $ACR_NAME

echo "✅ AKS cluster created successfully!"

# Step 2: Get AKS credentials
echo ""
echo "Step 2: Getting AKS credentials..."
az aks get-credentials \
  --resource-group $RESOURCE_GROUP \
  --name $AKS_NAME \
  --overwrite-existing

echo "✅ Credentials configured!"

# Step 3: Verify connection
echo ""
echo "Step 3: Verifying cluster connection..."
kubectl cluster-info
kubectl get nodes

# Step 4: Install NGINX Ingress Controller
echo ""
echo "Step 4: Installing NGINX Ingress Controller..."
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.9.4/deploy/static/provider/cloud/deploy.yaml

echo "Waiting for NGINX Ingress Controller to be ready..."
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=300s

echo "✅ NGINX Ingress Controller installed!"

# Step 5: Get Load Balancer Public IP
echo ""
echo "Step 5: Getting Load Balancer Public IP..."
sleep 30  # Wait for external IP assignment
EXTERNAL_IP=$(kubectl get svc -n ingress-nginx ingress-nginx-controller -o jsonpath='{.status.loadBalancer.ingress[0].ip}')

if [ -z "$EXTERNAL_IP" ]; then
    echo "⏳ Waiting for external IP to be assigned..."
    sleep 30
    EXTERNAL_IP=$(kubectl get svc -n ingress-nginx ingress-nginx-controller -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
fi

echo "✅ Load Balancer External IP: $EXTERNAL_IP"

# Step 6: Deploy the secret (update with your connection string)
echo ""
echo "Step 6: Creating Kubernetes secret..."
echo "⚠️  Please update k8s/secret.yaml with your actual database connection string before continuing"
read -p "Press Enter to continue after updating secret.yaml..."

kubectl apply -f k8s/secret.yaml
echo "✅ Secret created!"

# Step 7: Deploy the application
echo ""
echo "Step 7: Deploying Payment API..."
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/hpa.yaml

echo "Waiting for deployment to be ready..."
kubectl rollout status deployment/payment-api --timeout=300s
echo "✅ Payment API deployed!"

# Step 8: Deploy Ingress
echo ""
echo "Step 8: Deploying Ingress..."
kubectl apply -f k8s/ingress.yaml
echo "✅ Ingress deployed!"

# Step 9: Display deployment information
echo ""
echo "=========================================="
echo "🎉 Deployment Complete!"
echo "=========================================="
echo ""
echo "📊 Cluster Information:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  AKS Cluster: $AKS_NAME"
echo "  Location: $LOCATION"
echo ""
echo "🌐 Access Information:"
echo "  Load Balancer IP: $EXTERNAL_IP"
echo "  API URL: http://$EXTERNAL_IP"
echo "  Swagger: http://$EXTERNAL_IP/swagger"
echo ""
echo "📝 DNS Configuration:"
echo "  Create an A record pointing to: $EXTERNAL_IP"
echo "  Then update k8s/ingress.yaml with your domain"
echo ""
echo "🔍 Useful Commands:"
echo "  kubectl get pods"
echo "  kubectl get services"
echo "  kubectl get ingress"
echo "  kubectl logs -l app=payment-api"
echo "  kubectl describe ingress payment-api-ingress"
echo ""
echo "🧪 Test the API:"
echo "  curl http://$EXTERNAL_IP/swagger/index.html"
echo ""
