# Payment Processing API

A production-ready .NET 8 Web API for payment processing, deployed on Azure Kubernetes Service (AKS) with SQL Server database.

## 🚀 Live Production Deployment

**Base URL:** `http://4.213.208.156`

**Alternative DNS:** `http://payment-api.4.187.130.156.nip.io`

## ✨ Features

- ✅ Create, retrieve, capture, and cancel payment charges
- ✅ Idempotency key support to prevent duplicate transactions
- ✅ SQL Server 2022 database with persistent storage
- ✅ Clean MVC architecture (Controllers → Services → Data Layer)
- ✅ Production deployment on Azure Kubernetes Service
- ✅ Auto-scaling (2-10 pods based on load)
- ✅ Health monitoring and readiness probes
- ✅ Load-balanced external access

## 🏗️ Architecture

```
Internet → Azure LoadBalancer (4.213.208.156)
    ↓
Payment API Service (2-10 pods, auto-scaling)
    ↓
SQL Server 2022 (In-cluster, 8GB persistent storage)
```

## 🚢 Deployment Overview

- **Cloud Provider:** Microsoft Azure (Central India)
- **Container Registry:** demoimagecontainer.azurecr.io
- **Kubernetes Cluster:** payment-aks-cluster (2 nodes, Standard_B2s)
- **Database:** SQL Server 2022 (in-cluster)
- **Service Type:** LoadBalancer with External IP
- **Auto-scaling:** Horizontal Pod Autoscaler (HPA)

## 🏃 Quick Start

### Production API Usage

The API is live and ready to use at `http://4.213.208.156`

### Health Check

```bash
curl http://4.213.208.156/
# Response: {"status":"healthy","service":"Payment API","version":"1.0"}
```

### Local Development

**Prerequisites:**

- .NET 8 SDK
- Docker (for SQL Server)

**Setup & Run:**

1. **Start SQL Server:**

   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd123" \
      -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
   ```

2. **Apply Database Migrations:**

   ```bash
   cd WebApiProject
   dotnet ef database update
   ```

3. **Run the API:**
   ```bash
   dotnet run
   ```
   Local API: `http://localhost:5062` | Swagger: `http://localhost:5062/swagger`

## 📡 API Endpoints (Base: `/v1/payments`)

| Method | Endpoint                | Description                                                 |
| ------ | ----------------------- | ----------------------------------------------------------- |
| POST   | `/charge`               | Create a payment charge (requires `Idempotency-Key` header) |
| GET    | `/{payment_id}`         | Retrieve payment details                                    |
| PATCH  | `/{payment_id}/capture` | Capture a pending payment                                   |
| PATCH  | `/{payment_id}/cancel`  | Cancel an uncaptured payment                                |

### Example: Create Charge (Production)

```bash
curl -X POST http://4.213.208.156/v1/payments/charge \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: unique-key-$(date +%s)" \
  -d '{
    "amount": 2500,
    "currency": "USD",
    "description": "Order #12345",
    "customerId": "cust_001",
    "capture": false,
    "paymentMethod": {
      "type": "card",
      "cardNumber": "4242424242424242",
      "expiryMonth": 12,
      "expiryYear": 2025,
      "cvv": "123"
    }
  }'
```

**Note:** Amount is in **cents** (2500 = $25.00)

**Response:**

```json
{
  "id": "dacf155d-9461-4724-8005-bf31eb765d9d",
  "status": "succeeded",
  "amount": 2500,
  "currency": "USD",
  "description": "Order #12345",
  "customerId": "cust_001",
  "createdAt": "2025-01-28T10:15:30.123Z",
  "paymentMethodType": "card",
  "cardLast4": "4242",
  "isIdempotent": false
}
```

### Example: Get Payment

```bash
curl http://4.213.208.156/v1/payments/dacf155d-9461-4724-8005-bf31eb765d9d
```

### Example: Capture Payment

```bash
curl -X PATCH http://4.213.208.156/v1/payments/{payment-id}/capture \
  -H "Content-Type: application/json"
```

### Example: Cancel Payment

```bash
curl -X PATCH http://4.213.208.156/v1/payments/{payment-id}/cancel \
  -H "Content-Type: application/json"
```

## 🏛️ System Architecture

### Application Layer

```
Controllers/         # HTTP request handling (PaymentsController.cs)
    ↓
Services/           # Business logic (PaymentService, IdempotencyService)
    ↓
Data/               # EF Core DbContext + Database persistence
    ↓
Models/             # Entities (Charge, IdempotencyRecord) + DTOs
```

### Tech Stack

- .NET 8.0.121 | ASP.NET Core Web API
- Entity Framework Core 9.0.10
- SQL Server 2022
- Docker (multi-stage build)
- Azure Kubernetes Service
- Azure Container Registry

## 🗄️ Database

### Charges Table

```sql
CREATE TABLE Charges (
  Id UNIQUEIDENTIFIER PRIMARY KEY,
  Status NVARCHAR(50) NOT NULL,
  Amount BIGINT NOT NULL,              -- Amount in cents
  Currency NVARCHAR(10) NOT NULL,
  Description NVARCHAR(MAX),
  CustomerId NVARCHAR(100) NOT NULL,
  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
  PaymentMethodType NVARCHAR(50) NOT NULL,
  CardLast4 NVARCHAR(4)
);
```

### IdempotencyRecords Table

```sql
CREATE TABLE IdempotencyRecords (
  Id INT IDENTITY(1,1) PRIMARY KEY,
  IdempotencyKey NVARCHAR(255) NOT NULL UNIQUE,
  Response NVARCHAR(MAX) NOT NULL,
  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

**Connection String (Production):**

```
Server=mssql-service,1433;Database=PaymentDB;User Id=sa;Password=<from-secret>;TrustServerCertificate=True;
```

**Connection String (Local):**

```
Server=localhost,1433;Database=PaymentDB;User Id=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True;
```

## 💳 Payment Status Flow

```
pending → captured
  ↓
canceled
```

- **pending**: Created with `capture: false`, awaiting manual capture
- **succeeded**: Auto-captured (when `capture: true`, default)
- **captured**: Manually captured from pending state
- **canceled**: Payment canceled (cannot capture after cancellation)

## 🚢 Kubernetes Deployment

### Prerequisites

- Azure CLI installed
- kubectl installed
- Docker installed
- Access to Azure subscription

### Deploy to AKS

1. **Get AKS credentials:**

```bash
az aks get-credentials --resource-group Dev-Demo-App-Rg --name payment-aks-cluster
```

2. **Deploy all resources:**

```bash
# Deploy SQL Server
kubectl apply -f k8s/sqlserver-secret.yaml
kubectl apply -f k8s/sqlserver-pvc.yaml
kubectl apply -f k8s/sqlserver-deployment.yaml
kubectl apply -f k8s/sqlserver-service.yaml

# Initialize database
kubectl apply -f k8s/migration-job.yaml

# Deploy Payment API
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/hpa.yaml
```

3. **Or use the deployment script:**

```bash
chmod +x deploy-to-aks.sh
./deploy-to-aks.sh
```

### Build and Push Docker Image

```bash
# Login to ACR
az acr login --name demoimagecontainer

# Build and push
cd WebApiProject
docker buildx build --platform linux/amd64 \
  -t demoimagecontainer.azurecr.io/payment-api:latest \
  -t demoimagecontainer.azurecr.io/payment-api:v1.3 \
  --push .
```

## 📊 Monitoring & Operations

### Check Pod Status

```bash
kubectl get pods -l app=payment-api
kubectl get pods -l app=mssql
```

### View Logs

```bash
# API logs
kubectl logs -l app=payment-api -f

# Database logs
kubectl logs -l app=mssql -f
```

### Check Services

```bash
kubectl get svc
kubectl get hpa
```

### Restart Deployment

```bash
kubectl rollout restart deployment/payment-api
kubectl rollout restart deployment/mssql-deployment
```

## 📁 Project Structure

```
scalableassignmentps2/
├── WebApiProject/              # Main API project
│   ├── Controllers/            # PaymentsController.cs
│   ├── Services/              # PaymentService, IdempotencyService
│   ├── Models/                # Charge, ChargeRequest, ChargeResponse
│   ├── Data/                  # PaymentDbContext
│   ├── Filters/               # IdempotencyKeyHeaderFilter
│   └── Dockerfile             # Docker configuration
├── k8s/                       # Kubernetes manifests
│   ├── deployment.yaml        # API deployment (2 replicas)
│   ├── service.yaml           # LoadBalancer service
│   ├── hpa.yaml              # Auto-scaling (2-10 pods)
│   ├── secret.yaml           # API secrets
│   ├── sqlserver-*.yaml      # SQL Server resources
│   └── migration-job.yaml    # Database initialization
├── deploy-to-aks.sh          # Deployment script
├── API-ENDPOINTS.md          # API documentation
└── README.md                 # This file
```

## 📈 Scalability & Performance

- **Horizontal Pod Autoscaler:** Scales from 2-10 pods based on CPU (70%) and memory (80%)
- **Resource Limits:** API pods: 256Mi-512Mi memory, 250m-500m CPU
- **Persistent Storage:** 8GB Premium SSD for SQL Server
- **Load Balancing:** Azure LoadBalancer distributes traffic across pods
- **Health Checks:** Liveness and readiness probes ensure reliability

## 🔐 Security

- Database credentials stored as Kubernetes secrets
- SQL Server accessible only within cluster (ClusterIP)
- Idempotency keys prevent duplicate payments
- GUID-based payment IDs for unpredictability

## 🐛 Troubleshooting

### Pods not ready

```bash
kubectl describe pod -l app=payment-api
kubectl logs -l app=payment-api --tail=50
```

### Database connection issues

```bash
kubectl exec -it <mssql-pod> -- /bin/bash
```

### View all resources

```bash
kubectl get all
kubectl get pvc
kubectl get secrets
```

## 📝 Important Notes

- **Amount Format:** All amounts are in **cents** (integer). Example: 1000 = $10.00
- **Idempotency:** Use unique `Idempotency-Key` header for each new payment
- **Capture Flag:** Set `capture: false` to create pending payments for later capture
- **Payment IDs:** System generates GUID identifiers (e.g., `dacf155d-9461-4724-8005-bf31eb765d9d`)

## 🤝 Contributing

This is an academic project for Scalable Services Assignment PS2.

## 📄 License

Educational use only.

---

**Deployed on:** Azure Kubernetes Service (AKS)  
**Region:** Central India  
**Cluster:** payment-aks-cluster  
**Resource Group:** Dev-Demo-App-Rg  
**Status:** ✅ Production Ready
