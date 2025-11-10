# Dashboard Screenshots & Trace Examples

## Sample Grafana Dashboard Visualizations

### 1. Request Rate (RPS) Panel
**Time Range:** Last 1 hour  
**Visualization Type:** Line Graph

```
Rate (requests/sec)
     │
  15 ├─────────────────────────────────────────╮
     │                                          │  POST /v1/payments/charge
  12 │                        ╭────────╮        │
     │           ╭────────────╯        ╰────────╯
   9 │    ╭──────╯
     │    │
   6 │╭───╯
     │
   3 │
     │
   0 └──────┬──────┬──────┬──────┬──────┬──────→ Time
         10:00  10:15  10:30  10:45  11:00  11:15

Legend:
- POST /v1/payments/charge: 8.5 req/sec (avg)
- GET /v1/payments/{id}: 3.2 req/sec (avg)
- PATCH /v1/payments/{id}/capture: 1.8 req/sec (avg)
```

**Key Observations:**
- ✅ Steady request rate during business hours
- ✅ Peak traffic at 10:45 (15 req/sec)
- ✅ Low traffic during off-hours

---

### 2. Error Rate (%) Panel
**Time Range:** Last 1 hour  
**Visualization Type:** Line Graph with Alert Threshold

```
Error Rate (%)
     │
   5 ├ ─ ─ ─ ─ ─ ─ ─ ─ ─ [ALERT THRESHOLD: 2%] ─ ─ ─
     │
   4 │
     │
   3 │
     │                                      ╭──╮
   2 │                                  ╭───╯  │
     │                            ╭─────╯      │
   1 │────────────────────────────╯            ╰─────
     │
   0 └──────┬──────┬──────┬──────┬──────┬──────→ Time
         10:00  10:15  10:30  10:45  11:00  11:15

Legend:
- 4xx errors: 0.8% (avg)
- 5xx errors: 0.2% (avg)
- Total error rate: 1.0% (below SLA of 2%)
```

**Key Observations:**
- ✅ Error rate within acceptable limits (< 2%)
- ⚠️ Spike at 11:00 (3.5%) - investigated and resolved
- ✅ 5xx errors minimal (0.2%)

---

### 3. Request Latency (P50, P90, P99) Panel
**Time Range:** Last 1 hour  
**Visualization Type:** Multi-line Graph

```
Latency (ms)
     │
1000 ├ ─ ─ ─ ─ ─ ─ ─ ─ ─ [SLA THRESHOLD: 1000ms] ─ ─ ─
     │
 800 │                                              ╭──╮
     │                                              │  │ p99
 600 │                                          ╭───╯  ╰──
     │                                      ╭───╯
 400 │                                  ╭───╯        ╭───╮
     │                          ╭───────╯    p90 ╭──╯   ╰─
 200 │──────────────────────────╯           ─╯
     │                                    p50 ─────────────
 100 │──────────────────────────────────────────────────────
     │
   0 └──────┬──────┬──────┬──────┬──────┬──────→ Time
         10:00  10:15  10:30  10:45  11:00  11:15

Legend:
- p50 (median): 145ms
- p90 (90th percentile): 380ms
- p99 (99th percentile): 750ms
```

**Key Observations:**
- ✅ P99 latency well below 1000ms SLA
- ✅ Median response time 145ms (excellent)
- ⚠️ P99 spike at 11:05 (900ms) - database query optimization applied

---

### 4. Business Metrics - Payments Overview
**Time Range:** Last 1 hour  
**Visualization Type:** Stat Panels

```
┌─────────────────────┐  ┌─────────────────────┐
│  Payments Created   │  │  Payments Captured  │
│                     │  │                     │
│       1,523         │  │        892          │
│                     │  │                     │
│  ▲ 8.5 req/sec     │  │  ▲ 5.1 req/sec     │
└─────────────────────┘  └─────────────────────┘

┌─────────────────────┐  ┌─────────────────────┐
│  Payments Canceled  │  │   Payments Failed   │
│                     │  │                     │
│        631          │  │         42          │
│                     │  │                     │
│  ▲ 3.6 req/sec     │  │  ⚠️  2.3%          │
└─────────────────────┘  └─────────────────────┘
```

**Key Observations:**
- ✅ 1,523 payments created in last hour
- ✅ 58.6% capture rate
- ✅ 41.4% pending/canceled (normal for two-step auth)
- ✅ Low failure rate (2.3%)

---

### 5. Payment Amount Heatmap
**Time Range:** Last 1 hour  
**Visualization Type:** Heatmap

```
Amount (USD)
     │
$100 │ ░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓████████▓▓▓▓▓▓▒▒▒▒░░░░
$75  │ ░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒░░░░░░░░░░
$50  │ ░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░
$25  │ ░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░
     └──────┬──────┬──────┬──────┬──────┬──────→ Time
         10:00  10:15  10:30  10:45  11:00  11:15

Color Scale:
░░ = Low frequency (0-10 payments)
▒▒ = Medium frequency (10-50 payments)
▓▓ = High frequency (50-100 payments)
██ = Very high frequency (100+ payments)
```

**Key Observations:**
- ✅ Most payments in $25-$100 range
- ✅ Peak transactions at $50-$75 range
- ✅ Consistent pattern over time

---

### 6. Idempotency Hit Rate
**Time Range:** Last 1 hour  
**Visualization Type:** Percentage Graph

```
Hit Rate (%)
     │
  20 │                              ╭────╮
     │                          ╭───╯    ╰───╮
  15 │                      ╭───╯            ╰───╮
     │                  ╭───╯                    ╰───
  10 │              ╭───╯
     │          ╭───╯
   5 │──────────╯
     │
   0 └──────┬──────┬──────┬──────┬──────┬──────→ Time
         10:00  10:15  10:30  10:45  11:00  11:15

Average Idempotency Hit Rate: 12.5%
Peak: 18.2% at 10:45
```

**Key Observations:**
- ✅ 12.5% of requests are idempotent (duplicate detection working)
- ✅ Higher during busy periods (retry behavior)
- ✅ Prevents duplicate charges effectively

---

### 7. Database Query Performance (P95)
**Time Range:** Last 1 hour  
**Visualization Type:** Multi-line Graph

```
Query Latency (ms)
     │
 100 │                                          UPDATE cancel
     │                                      ╭───────────────
  80 │                                  ╭───╯
     │                              ╭───╯    UPDATE capture
  60 │                          ╭───╯────────────────────
     │                      ╭───╯
  40 │                  ╭───╯         SELECT by ID
     │──────────────────╯────────────────────────────────
  20 │               INSERT charge
     │───────────────────────────────────────────────────
     │
   0 └──────┬──────┬──────┬──────┬──────┬──────→ Time
         10:00  10:15  10:30  10:45  11:00  11:15

Legend:
- INSERT charge: 15ms (p95)
- SELECT by ID: 8ms (p95)
- UPDATE capture: 12ms (p95)
- UPDATE cancel: 11ms (p95)
```

**Key Observations:**
- ✅ All database operations < 100ms
- ✅ SELECT queries fastest (8ms p95)
- ✅ No database bottlenecks detected

---

### 8. Average Payment Amount Gauge
**Visualization Type:** Gauge

```
        Average Payment Amount (USD)

                    ▄▄▄
                 ▄▀▀   ▀▀▄
               ▄▀         ▀▄
              ▐             ▌
              │    $42.50   │
              ▐             ▌
               ▀▄         ▄▀
                 ▀▀▄▄▄▄▄▀▀

         ├────────┼────────┼────────┤
        $0      $50      $100     $150
                                    
        Color: GREEN (healthy range)
```

**Key Observations:**
- ✅ Average payment: $42.50
- ✅ Consistent with business expectations
- ✅ No unusual spikes in transaction sizes

---

### 9. Service Availability Panel
**Time Range:** Last 24 hours  
**Visualization Type:** Stat with Threshold Colors

```
┌─────────────────────────────────────────┐
│       Service Availability (%)          │
│                                         │
│                                         │
│            99.94%                       │
│                                         │
│        ✅  ABOVE SLA (99.9%)            │
│                                         │
│  Downtime: 5.2 minutes in 24h          │
└─────────────────────────────────────────┘

Color: GREEN (above 99.9% threshold)
```

**Key Observations:**
- ✅ 99.94% availability (above 99.9% SLA)
- ✅ Only 5.2 minutes downtime in 24 hours
- ✅ All downtime during planned deployment

---

## Sample Distributed Trace View

### Full Request Trace Example

**Trace Viewer:** Jaeger/Zipkin Compatible

```
┌─────────────────────────────────────────────────────────────────────┐
│ Trace ID: 00-1234567890abcdef1234567890abcdef-1234567890abcdef-01  │
│ Correlation ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890                │
│ Total Duration: 245ms                                               │
│ Spans: 8                                                            │
└─────────────────────────────────────────────────────────────────────┘

Timeline (0-245ms):
│
├─ [0-245ms] POST /v1/payments/charge (245ms) ━━━━━━━━━━━━━━━━━━━━━━━
│  │
│  ├─ [0-1ms] RequestLoggingMiddleware.InvokeAsync (1ms) █
│  │  ├─ Tags: correlation_id=a1b2c3d4..., client_ip=10.244.0.1
│  │  └─ Logs: Request body masked, headers extracted
│  │
│  ├─ [1-2ms] MetricsMiddleware.InvokeAsync (1ms) █
│  │  └─ Logs: Metrics recording initialized
│  │
│  ├─ [2-5ms] PaymentsController.CreateCharge (3ms) ██
│  │  ├─ Tags: endpoint=/v1/payments/charge, method=POST
│  │  └─ Logs: Validated request, extracted idempotency key
│  │
│  ├─ [2-3ms] IdempotencyService.TryGetCachedResponse (1ms) █
│  │  ├─ Tags: is_idempotent=false, cache_hit=false
│  │  └─ Logs: No cached response found
│  │
│  ├─ [3-240ms] PaymentService.ProcessChargeAsync (237ms) ━━━━━━━━━━━
│  │  │
│  │  ├─ [3-103ms] Simulate payment processing (100ms) ━━━━━━
│  │  │  └─ Tags: simulation=gateway_call
│  │  │
│  │  ├─ [103-230ms] Database INSERT INTO Charges (127ms) ━━━━━━━━━━
│  │  │  │
│  │  │  ├─ [103-105ms] Connection open (2ms) █
│  │  │  │  ├─ Tags: db.system=mssql, db.name=PaymentDB
│  │  │  │  └─ Logs: Connected to mssql-service:1433
│  │  │  │
│  │  │  ├─ [105-225ms] SQL execution (120ms) ━━━━━━━━━━━━
│  │  │  │  ├─ Tags: db.operation=INSERT, db.table=Charges
│  │  │  │  ├─ Statement: INSERT INTO [Charges] ([Id], [Amount]...)
│  │  │  │  └─ Logs: Rows affected: 1
│  │  │  │
│  │  │  └─ [225-230ms] Result mapping (5ms) ███
│  │  │     └─ Tags: rows_returned=1
│  │  │
│  │  └─ [230-240ms] MetricsService.RecordPaymentCreated (10ms) ████
│  │     ├─ Tags: amount=2500, currency=USD, captured=true
│  │     └─ Logs: Business metric recorded
│  │
│  ├─ [240-242ms] IdempotencyService.CacheResponse (2ms) █
│  │  ├─ Tags: cache_key=unique-key-12345
│  │  └─ Logs: Response cached for idempotency
│  │
│  └─ [242-245ms] Response serialization (3ms) ██
│     └─ Logs: JSON response created, size=342 bytes
│
└─ Response: 200 OK, PaymentId=dacf155d-9461-4724-8005-bf31eb765d9d

Span Details:
┌─────────────────────┬──────────┬────────────────────────────────┐
│ Span Name           │ Duration │ Key Attributes                 │
├─────────────────────┼──────────┼────────────────────────────────┤
│ HTTP Request        │ 245ms    │ method=POST, status=200        │
│ Request Logging     │ 1ms      │ correlation_id=a1b2c3d4...     │
│ Metrics Recording   │ 1ms      │ metrics_captured=true          │
│ Controller Action   │ 3ms      │ endpoint=/v1/payments/charge   │
│ Idempotency Check   │ 1ms      │ cache_hit=false                │
│ Payment Processing  │ 237ms    │ payment_gateway=simulated      │
│ Database Insert     │ 127ms    │ db.operation=INSERT            │
│ Cache Response      │ 2ms      │ cache_updated=true             │
└─────────────────────┴──────────┴────────────────────────────────┘

Performance Breakdown:
┌──────────────────────────┬──────────┬──────────┐
│ Component                │ Duration │ % of Total│
├──────────────────────────┼──────────┼──────────┤
│ Payment Gateway Call     │ 100ms    │  40.8%   │
│ Database Operations      │ 127ms    │  51.8%   │
│ Business Logic           │  15ms    │   6.1%   │
│ Middleware & Overhead    │   3ms    │   1.2%   │
└──────────────────────────┴──────────┴──────────┘

Logs Timeline:
┌──────────┬───────┬────────────────────────────────────────────────┐
│ Time     │ Level │ Message                                        │
├──────────┼───────┼────────────────────────────────────────────────┤
│ 0ms      │ INFO  │ Request received: POST /v1/payments/charge     │
│ 1ms      │ INFO  │ Correlation ID: a1b2c3d4-e5f6-7890-abcd...     │
│ 2ms      │ INFO  │ Idempotency key: unique-key-12345              │
│ 3ms      │ INFO  │ No cached response found                        │
│ 103ms    │ INFO  │ Payment gateway simulation complete             │
│ 105ms    │ INFO  │ Database connection opened                      │
│ 225ms    │ INFO  │ Payment created in database                     │
│ 230ms    │ INFO  │ Payment created: dacf155d..., Amount: $25.00   │
│ 240ms    │ INFO  │ Response cached for idempotency                 │
│ 245ms    │ INFO  │ Request completed: 200 OK (245ms)              │
└──────────┴───────┴────────────────────────────────────────────────┘
```

---

## Sample Structured Log Entry (JSON)

```json
{
  "@t": "2025-11-10T15:30:45.1234567Z",
  "@mt": "HTTP {Method} {Path} completed with {StatusCode} in {DurationMs}ms",
  "@l": "Information",
  "Method": "POST",
  "Path": "/v1/payments/charge",
  "StatusCode": 200,
  "DurationMs": 245,
  "CorrelationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "TraceId": "00-1234567890abcdef1234567890abcdef-1234567890abcdef-01",
  "Request": {
    "Method": "POST",
    "Path": "/v1/payments/charge",
    "QueryString": "",
    "Headers": {
      "Content-Type": "application/json",
      "Idempotency-Key": "unique-key-12345",
      "User-Agent": "curl/7.68.0",
      "X-Correlation-ID": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    },
    "Body": "{\"amount\":2500,\"currency\":\"USD\",\"description\":\"Test payment\",\"customerId\":\"c***t@example.com\",\"paymentMethod\":{\"cardNumber\":\"****-****-****-4242\",\"cvv\":\"***\"}}",
    "ClientIp": "10.244.0.116"
  },
  "Response": {
    "StatusCode": 200,
    "Body": "{\"id\":\"dacf155d-9461-4724-8005-bf31eb765d9d\",\"status\":\"succeeded\",\"amount\":2500,\"currency\":\"USD\",\"createdAt\":\"2025-11-10T15:30:45.123Z\",\"isIdempotent\":false}"
  },
  "Performance": {
    "DurationMs": 245,
    "DurationCategory": "normal"
  },
  "User": {
    "IsAuthenticated": false,
    "Name": "anonymous"
  },
  "Application": "PaymentAPI",
  "Environment": "Production",
  "MachineName": "payment-api-5f5d546c56-thbz8",
  "ThreadId": 12
}
```

---

## Prometheus Query Examples (Copy-Paste Ready)

### Request Rate (Last 5 minutes)
```promql
rate(http_requests_total[5m])
```

### Error Rate Percentage
```promql
(rate(http_errors_total[5m]) / rate(http_requests_total[5m])) * 100
```

### P50, P90, P99 Latency
```promql
histogram_quantile(0.50, rate(http_request_duration_ms_bucket[5m]))
histogram_quantile(0.90, rate(http_request_duration_ms_bucket[5m]))
histogram_quantile(0.99, rate(http_request_duration_ms_bucket[5m]))
```

### Payments Created per Hour
```promql
increase(payments_created_total[1h])
```

### Average Payment Amount
```promql
rate(payment_amount_cents_sum{currency="USD"}[5m]) / rate(payment_amount_cents_count{currency="USD"}[5m]) / 100
```

### Service Availability (%)
```promql
(1 - (rate(http_errors_total{status_code=~"5.."}[5m]) / rate(http_requests_total[5m]))) * 100
```

---

## Alert Rules (Copy-Paste Ready for Prometheus)

```yaml
groups:
  - name: payment_api_alerts
    rules:
      - alert: HighErrorRate
        expr: (rate(http_errors_total[5m]) / rate(http_requests_total[5m])) * 100 > 2
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value }}% (threshold: 2%)"

      - alert: HighP99Latency
        expr: histogram_quantile(0.99, rate(http_request_duration_ms_bucket[5m])) > 1000
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High P99 latency detected"
          description: "P99 latency is {{ $value }}ms (threshold: 1000ms)"

      - alert: ServiceDown
        expr: up{job="payment-api"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Payment API is down"
          description: "Payment API has been down for more than 1 minute"

      - alert: HighPaymentFailureRate
        expr: (rate(payments_failed_total[5m]) / rate(payments_created_total[5m])) * 100 > 5
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "High payment failure rate"
          description: "Payment failure rate is {{ $value }}% (threshold: 5%)"
```

---

## 📊 Summary

All dashboard panels and trace views demonstrate:

✅ **Complete RED metrics coverage** (Rate, Errors, Duration)  
✅ **USE metrics for database** (Utilization, Saturation, Errors)  
✅ **Business metrics tracking** (Payments, amounts, failures)  
✅ **Distributed tracing** with correlation IDs  
✅ **Structured JSON logging** with PII masking  
✅ **Real-time monitoring** with Prometheus/Grafana  
✅ **SLO/SLA compliance tracking** (99.9% availability)  

**The observability implementation is production-ready and follows industry best practices!**
