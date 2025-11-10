# Payment API - Public Access URLs

## 🌐 Public DNS Access

**Base URL:** `http://payment-api.4.187.130.156.nip.io`

### Available Endpoints

#### Health Check

```bash
curl http://payment-api.4.187.130.156.nip.io/
```

**Response:** `{"status":"healthy","service":"Payment API","version":"1.0"}`

#### Create Payment Charge

```bash
curl -X POST http://payment-api.4.187.130.156.nip.io/v1/payments/charge \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: unique-key-123" \
  -d '{
    "amount": 1000,
    "currency": "USD",
    "description": "Test payment",
    "customerId": "cust_123",
    "paymentMethod": {
      "type": "card",
      "cardNumber": "4242424242424242",
      "expiryMonth": 12,
      "expiryYear": 2025,
      "cvv": "123"
    }
  }'
```

#### Get Payment by ID

```bash
curl http://payment-api.4.187.130.156.nip.io/v1/payments/{id}
```

#### Capture Payment

```bash
curl -X PATCH http://payment-api.4.187.130.156.nip.io/v1/payments/{id}/capture \
  -H "Content-Type: application/json"
```

#### Cancel Payment

```bash
curl -X PATCH http://payment-api.4.187.130.156.nip.io/v1/payments/{id}/cancel \
  -H "Content-Type: application/json"
```

---

## 📊 Alternative Access Methods

### Direct IP Access (No DNS)

```bash
curl http://4.187.130.156/
```

### Load Balancer IP

**IP Address:** `4.187.130.156`

---

## 🔧 Technical Details

- **Kubernetes Cluster:** payment-aks-cluster
- **Resource Group:** Dev-Demo-App-Rg
- **Region:** Central India
- **Ingress Controller:** NGINX
- **DNS Provider:** nip.io (wildcard DNS service)
- **Running Pods:** 2 replicas
- **Platform:** Azure Kubernetes Service (AKS)

---

## ⚠️ Important Notes

1. **Database Connectivity:** Currently, payment endpoints will timeout because the app is configured to connect to localhost SQL Server. To fix this:

   - Set up Azure SQL Database, OR
   - Deploy SQL Server in AKS cluster

2. **HTTP Only:** Currently using HTTP. For production, configure HTTPS with cert-manager and Let's Encrypt.

3. **nip.io DNS:** This is a free wildcard DNS service. For production, use your own domain with Azure DNS Zone.

---

## 🚀 Next Steps

### For Production Deployment:

1. **Setup Database:**

   ```bash
   # Option 1: Create Azure SQL Database
   az sql server create --name payment-sql-server --resource-group Dev-Demo-App-Rg --location centralindia --admin-user sqladmin --admin-password 'YourPassword123!'
   az sql db create --resource-group Dev-Demo-App-Rg --server payment-sql-server --name PaymentDB --service-objective Basic
   ```

2. **Configure Custom Domain:**

   - Register a domain (e.g., api.yourdomain.com)
   - Create Azure DNS Zone
   - Add A record pointing to `4.187.130.156`

3. **Enable HTTPS:**

   ```bash
   kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml
   # Then configure Let's Encrypt issuer
   ```

4. **Update Connection String:**

   ```bash
   kubectl create secret generic payment-api-secret \
     --from-literal=db-connection-string="Server=payment-sql-server.database.windows.net,1433;Database=PaymentDB;User ID=sqladmin;Password=YourPassword123!;Encrypt=True;" \
     --dry-run=client -o yaml | kubectl apply -f -

   kubectl rollout restart deployment payment-api
   ```
