# Observability Implementation - Payment API

## Golden Signals Implementation

This document describes the comprehensive observability implementation for the Payment Processing API, following industry best practices with Golden Signals (RED/USE metrics), structured logging, and distributed tracing.

---

## 📊 Metrics (RED + USE + Business)

### RED Metrics (Request-focused)

#### Rate - Request Volume
```
http_requests_total{method="POST", path="/v1/payments/charge", status_code="200"}
http_requests_total{method="GET", path="/v1/payments/{id}", status_code="200"}
http_requests_total{method="PATCH", path="/v1/payments/{id}/capture", status_code="200"}
```

**Labels:**
- `method`: HTTP method (GET, POST, PATCH)
- `path`: Normalized API path (IDs replaced with `{id}`)
- `status_code`: HTTP status code
- `status_category`: Status category (2xx, 3xx, 4xx, 5xx)

#### Errors - Error Rate
```
http_errors_total{method="POST", path="/v1/payments/charge", status_code="500"}
http_errors_total{method="PATCH", path="/v1/payments/{id}/capture", status_code="400"}
```

**Labels:**
- Same as request metrics
- Only incremented for status codes >= 400

#### Duration - Request Latency
```
http_request_duration_ms{method="POST", path="/v1/payments/charge", status_code="200"}
```

**Histogram buckets:**
- p50 (median)
- p90 (90th percentile)
- p95 (95th percentile)
- p99 (99th percentile)

### USE Metrics (Resource-focused)

#### Utilization
```
database_query_duration_ms{query_type="insert_charge", success="true"}
database_query_duration_ms{query_type="select_charge_by_id", success="true"}
database_query_duration_ms{query_type="update_charge_capture", success="true"}
database_query_duration_ms{query_type="update_charge_cancel", success="true"}
```

#### Saturation & Errors
```
database_connection_errors_total{query_type="insert_charge"}
```

### Business Metrics

#### Payment Operations
```
payments_created_total{currency="USD", captured="true"}
payments_created_total{currency="USD", captured="false"}
payments_captured_total{currency="USD"}
payments_canceled_total{currency="USD"}
payments_failed_total{reason="processing_error", currency="USD"}
payments_failed_total{reason="already_canceled", currency="USD"}
payments_failed_total{reason="invalid_status", currency="USD"}
```

#### Payment Amount Distribution
```
payment_amount_cents{currency="USD"}
```
**Histogram:** Tracks payment amount distribution for revenue analysis

#### Idempotency Performance
```
idempotency_check_latency_ms{is_idempotent="true"}
idempotency_check_latency_ms{is_idempotent="false"}
```

---

## 📝 Structured Logging

### Log Format
All logs are in **structured JSON format** using Serilog with Compact JSON Formatter.

### Log Fields

#### Standard Fields (Every Log Entry)
```json
{
  "Timestamp": "2025-11-10T15:30:45.123Z",
  "Level": "Information",
  "MessageTemplate": "HTTP {Method} {Path} completed...",
  "Properties": {
    "Application": "PaymentAPI",
    "Environment": "Production",
    "MachineName": "payment-api-5f5d546c56-thbz8",
    "ThreadId": 12
  }
}
```

#### Request/Response Logs
```json
{
  "Timestamp": "2025-11-10T15:30:45.123Z",
  "CorrelationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "TraceId": "00-1234567890abcdef1234567890abcdef-1234567890abcdef-01",
  "Request": {
    "Method": "POST",
    "Path": "/v1/payments/charge",
    "QueryString": "",
    "Headers": {
      "Content-Type": "application/json",
      "Idempotency-Key": "unique-key-12345",
      "User-Agent": "curl/7.68.0"
    },
    "Body": "{\"amount\":2500,\"currency\":\"USD\",\"customerId\":\"c***t@example.com\"}",
    "ClientIp": "10.244.0.1"
  },
  "Response": {
    "StatusCode": 200,
    "Body": "{\"id\":\"dacf155d-9461-4724-8005-bf31eb765d9d\",\"status\":\"succeeded\"...}"
  },
  "Performance": {
    "DurationMs": 245,
    "DurationCategory": "normal"
  },
  "User": {
    "IsAuthenticated": false,
    "Name": "anonymous"
  }
}
```

### PII Masking

**Automatically Masked Data:**

#### Email Addresses
```
Before: customer@example.com
After:  c***r@example.com
```

#### Phone Numbers
```
Before: 555-123-4567
After:  ***-***-****
```

#### Credit Card Numbers
```
Before: 4242-4242-4242-4242
After:  ****-****-****-4242  (last 4 digits retained)
```

#### CVV Codes
```json
Before: {"cvv": "123"}
After:  {"cvv": "***"}
```

#### Passwords
```json
Before: {"password": "secret123"}
After:  {"password": "***"}
```

### Duration Categories

Requests are automatically categorized by response time:

| Duration | Category | Log Level |
|----------|----------|-----------|
| < 100ms  | fast | Information |
| < 500ms  | normal | Information |
| < 1000ms | slow | Information |
| < 5000ms | very_slow | Warning |
| >= 5000ms | critical | Warning |

### Business Event Logs

#### Payment Created
```
INFO: Payment created: {PaymentId}, Amount: {Amount} {Currency}, Status: {Status}, Customer: {CustomerId}
```

#### Payment Captured
```
INFO: Payment captured: {PaymentId}, Amount: {Amount} {Currency}
```

#### Payment Canceled
```
INFO: Payment canceled: {PaymentId}, Amount: {Amount} {Currency}
```

#### Idempotency Hit
```
INFO: Idempotency key found in cache: {IdempotencyKey}, PaymentId: {PaymentId}
```

---

## 🔍 Distributed Tracing

### Trace Context Propagation

Every request is assigned:
1. **Correlation ID**: Business-level identifier
2. **Trace ID**: OpenTelemetry W3C trace ID

These IDs are:
- Generated automatically if not provided
- Propagated through all service layers
- Returned in response headers
- Logged with every operation

### Response Headers
```
X-Correlation-ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
X-Trace-ID: 00-1234567890abcdef1234567890abcdef-1234567890abcdef-01
```

### Trace Spans

#### HTTP Request Span
```
Span: POST /v1/payments/charge
├─ Duration: 245ms
├─ Status: 200 OK
├─ correlation_id: a1b2c3d4...
└─ Attributes:
   ├─ http.method: POST
   ├─ http.url: /v1/payments/charge
   ├─ http.status_code: 200
   └─ http.user_agent: curl/7.68.0
```

#### Database Query Span
```
Span: SELECT * FROM Charges
├─ Duration: 12ms
├─ Parent: POST /v1/payments/charge
└─ Attributes:
   ├─ db.system: mssql
   ├─ db.name: PaymentDB
   ├─ db.statement: SELECT [c].[Id]...
   └─ db.operation: SELECT
```

---

## 📍 Metrics Endpoint

### Prometheus Metrics Scraping

**Endpoint:** `/metrics`

**Format:** Prometheus exposition format

**Example Output:**
```
# HELP http_requests_total Total number of HTTP requests
# TYPE http_requests_total counter
http_requests_total{method="POST",path="/v1/payments/charge",status_code="200",status_category="2xx"} 1523

# HELP http_request_duration_ms Duration of HTTP requests in milliseconds
# TYPE http_request_duration_ms histogram
http_request_duration_ms_bucket{method="POST",path="/v1/payments/charge",status_code="200",le="100"} 456
http_request_duration_ms_bucket{method="POST",path="/v1/payments/charge",status_code="200",le="250"} 982
http_request_duration_ms_bucket{method="POST",path="/v1/payments/charge",status_code="200",le="500"} 1401
http_request_duration_ms_bucket{method="POST",path="/v1/payments/charge",status_code="200",le="+Inf"} 1523
http_request_duration_ms_sum{method="POST",path="/v1/payments/charge",status_code="200"} 234567.89
http_request_duration_ms_count{method="POST",path="/v1/payments/charge",status_code="200"} 1523

# HELP payments_created_total Total number of payments created
# TYPE payments_created_total counter
payments_created_total{currency="USD",captured="true"} 892
payments_created_total{currency="USD",captured="false"} 631

# HELP payments_captured_total Total number of payments captured
# TYPE payments_captured_total counter
payments_captured_total{currency="USD"} 589

# HELP payments_canceled_total Total number of payments canceled
# TYPE payments_canceled_total counter
payments_canceled_total{currency="USD"} 42

# HELP payments_failed_total Total number of failed payment attempts
# TYPE payments_failed_total counter
payments_failed_total{reason="processing_error",currency="USD"} 15
payments_failed_total{reason="already_canceled",currency="USD"} 8

# HELP payment_amount_cents Distribution of payment amounts in cents
# TYPE payment_amount_cents histogram
payment_amount_cents_bucket{currency="USD",le="1000"} 234
payment_amount_cents_bucket{currency="USD",le="5000"} 567
payment_amount_cents_bucket{currency="USD",le="10000"} 789
payment_amount_cents_bucket{currency="USD",le="+Inf"} 892

# HELP idempotency_check_latency_ms Time taken to check idempotency
# TYPE idempotency_check_latency_ms histogram
idempotency_check_latency_ms_bucket{is_idempotent="true",le="1"} 45
idempotency_check_latency_ms_bucket{is_idempotent="false",le="1"} 1478

# HELP database_query_duration_ms Duration of database queries in milliseconds
# TYPE database_query_duration_ms histogram
database_query_duration_ms_bucket{query_type="insert_charge",success="true",le="50"} 789
database_query_duration_ms_bucket{query_type="insert_charge",success="true",le="100"} 1456
```

---

## 🎯 Query Examples for Dashboards

### Request Rate (RPS)
```promql
rate(http_requests_total[5m])
```

### Error Rate (%)
```promql
rate(http_errors_total[5m]) / rate(http_requests_total[5m]) * 100
```

### P50 Latency
```promql
histogram_quantile(0.50, rate(http_request_duration_ms_bucket[5m]))
```

### P90 Latency
```promql
histogram_quantile(0.90, rate(http_request_duration_ms_bucket[5m]))
```

### P99 Latency
```promql
histogram_quantile(0.99, rate(http_request_duration_ms_bucket[5m]))
```

### Payment Success Rate
```promql
rate(payments_created_total[5m]) - rate(payments_failed_total[5m])
```

### Average Payment Amount (USD)
```promql
rate(payment_amount_cents_sum{currency="USD"}[5m]) / rate(payment_amount_cents_count{currency="USD"}[5m]) / 100
```

### Database Query Performance
```promql
histogram_quantile(0.95, rate(database_query_duration_ms_bucket[5m]))
```

---

## 📊 Recommended Dashboard Panels

### 1. Request Rate Panel (Line Graph)
- **Query:** `rate(http_requests_total{path="/v1/payments/charge"}[5m])`
- **Y-axis:** Requests per second
- **Legend:** Group by method

### 2. Error Rate Panel (Line Graph)
- **Query:** `rate(http_errors_total[5m]) by (status_code)`
- **Y-axis:** Errors per second
- **Legend:** Group by status code
- **Alert:** Threshold at 1% error rate

### 3. Latency Panel (Line Graph)
- **Queries:**
  - P50: `histogram_quantile(0.50, rate(http_request_duration_ms_bucket[5m]))`
  - P90: `histogram_quantile(0.90, rate(http_request_duration_ms_bucket[5m]))`
  - P99: `histogram_quantile(0.99, rate(http_request_duration_ms_bucket[5m]))`
- **Y-axis:** Milliseconds
- **Alert:** P99 > 1000ms

### 4. Business Metrics Panel (Stat)
- **Payments Created:** `increase(payments_created_total[1h])`
- **Payments Captured:** `increase(payments_captured_total[1h])`
- **Payments Canceled:** `increase(payments_canceled_total[1h])`
- **Payments Failed:** `increase(payments_failed_total[1h])`

### 5. Payment Amount Heatmap
- **Query:** `rate(payment_amount_cents_bucket[5m])`
- **Visualization:** Heatmap
- **Shows:** Payment amount distribution over time

### 6. Idempotency Hit Rate
- **Query:** `rate(idempotency_check_latency_ms_count{is_idempotent="true"}[5m]) / rate(idempotency_check_latency_ms_count[5m]) * 100`
- **Y-axis:** Percentage
- **Shows:** How many requests are duplicates

---

## 🔍 Sample Trace View

### Full Request Trace Example

```
Trace ID: 00-1234567890abcdef1234567890abcdef-1234567890abcdef-01
Correlation ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890

Timeline (Total: 245ms):
├─ [0-245ms] HTTP POST /v1/payments/charge
│  ├─ [0-1ms] RequestLoggingMiddleware.InvokeAsync
│  ├─ [1-2ms] MetricsMiddleware.InvokeAsync
│  ├─ [2-5ms] PaymentsController.CreateCharge
│  │  ├─ [2-3ms] IdempotencyService.TryGetCachedResponse
│  │  ├─ [3-240ms] PaymentService.ProcessChargeAsync
│  │  │  ├─ [3-103ms] Simulate payment processing (100ms delay)
│  │  │  ├─ [103-230ms] Database INSERT INTO Charges
│  │  │  │  ├─ [103-105ms] Connection open
│  │  │  │  ├─ [105-225ms] SQL execution
│  │  │  │  └─ [225-230ms] Result mapping
│  │  │  └─ [230-240ms] MetricsService.RecordPaymentCreated
│  │  └─ [240-242ms] IdempotencyService.CacheResponse
│  └─ [242-245ms] Response serialization

Tags:
├─ http.method: POST
├─ http.url: /v1/payments/charge
├─ http.status_code: 200
├─ correlation_id: a1b2c3d4-e5f6-7890-abcd-ef1234567890
├─ payment.id: dacf155d-9461-4724-8005-bf31eb765d9d
├─ payment.amount: 2500
├─ payment.currency: USD
├─ db.system: mssql
├─ db.name: PaymentDB
└─ db.operation: INSERT
```

---

## 📁 Log File Location

**Path:** `/logs/payment-api-YYYYMMDD.log`

**Rotation:**
- Daily rotation
- 7 days retention
- 100MB per file limit
- Compressed JSON format

**Example:**
```
/logs/payment-api-20251110.log
/logs/payment-api-20251109.log
/logs/payment-api-20251108.log
```

---

## 🚀 Testing Observability

### Generate Test Traffic
```bash
# Create payment
for i in {1..100}; do
  curl -X POST http://4.213.208.156/v1/payments/charge \
    -H "Content-Type: application/json" \
    -H "Idempotency-Key: test-key-$i" \
    -d '{
      "amount": '$((RANDOM % 10000 + 1000))',
      "currency": "USD",
      "description": "Load test payment '$i'",
      "customerId": "load-test-user",
      "capture": true,
      "paymentMethod": {
        "type": "card",
        "cardNumber": "4242424242424242",
        "expiryMonth": 12,
        "expiryYear": 2025,
        "cvv": "123"
      }
    }' &
done
wait
```

### Check Metrics
```bash
curl http://4.213.208.156/metrics | grep payments_created_total
curl http://4.213.208.156/metrics | grep http_request_duration_ms
```

### View Logs
```bash
kubectl logs -l app=payment-api -f | jq '.'
```

### Check Correlation IDs
```bash
curl -v http://4.213.208.156/v1/payments/charge \
  -H "X-Correlation-ID: my-custom-trace-123" \
  -H "Content-Type: application/json" \
  -d '{"amount":1000, "currency":"USD", "customerId":"test"}'
```

---

## 📊 SLO/SLA Targets

### Recommended Service Level Objectives

| Metric | Target | Alert Threshold |
|--------|--------|-----------------|
| **Availability** | 99.9% | < 99.5% |
| **P99 Latency** | < 500ms | > 1000ms |
| **Error Rate** | < 1% | > 2% |
| **Payment Success Rate** | > 99% | < 98% |

---

## ✅ Summary

This Payment API implements comprehensive observability with:

✅ **RED Metrics**: Request rate, errors, duration  
✅ **USE Metrics**: Database utilization, saturation, errors  
✅ **Business Metrics**: Payment operations, amounts, failures  
✅ **Structured Logging**: JSON format with correlation/trace IDs  
✅ **PII Masking**: Email, phone, card numbers automatically masked  
✅ **Distributed Tracing**: W3C trace context propagation  
✅ **Prometheus Export**: `/metrics` endpoint for scraping  
✅ **Request Correlation**: Track requests across all layers  

**All metrics and logs are production-ready and following industry best practices!**
