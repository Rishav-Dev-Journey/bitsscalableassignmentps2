#!/bin/bash

# Payment API - Observability Test Script
# Generates load to test metrics, logging, and tracing

set -e

BASE_URL=${1:-"http://4.213.208.156"}
NUM_REQUESTS=${2:-50}

echo "🚀 Testing Payment API Observability"
echo "Base URL: $BASE_URL"
echo "Number of requests: $NUM_REQUESTS"
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}📊 Testing RED Metrics (Rate, Errors, Duration)${NC}"
echo ""

# Test 1: Normal Payment Creation (Rate & Duration)
echo -e "${GREEN}Test 1: Creating $NUM_REQUESTS payments...${NC}"
for i in $(seq 1 $NUM_REQUESTS); do
  AMOUNT=$((RANDOM % 10000 + 1000))
  curl -s -X POST "$BASE_URL/v1/payments/charge" \
    -H "Content-Type: application/json" \
    -H "Idempotency-Key: load-test-$i-$(date +%s)" \
    -H "X-Correlation-ID: test-correlation-$i" \
    -d "{
      \"amount\": $AMOUNT,
      \"currency\": \"USD\",
      \"description\": \"Load test payment $i\",
      \"customerId\": \"load-test-customer-$((i % 10))\",
      \"capture\": $((RANDOM % 2 == 0)),
      \"paymentMethod\": {
        \"type\": \"card\",
        \"cardNumber\": \"4242424242424242\",
        \"expiryMonth\": 12,
        \"expiryYear\": 2025,
        \"cvv\": \"123\"
      }
    }" > /dev/null &
  
  # Limit concurrent requests
  if [ $((i % 10)) -eq 0 ]; then
    wait
    echo "  ✓ Completed $i/$NUM_REQUESTS payments"
  fi
done
wait
echo -e "${GREEN}✓ Completed all payment creations${NC}"
echo ""

# Test 2: Idempotency (Business Metrics)
echo -e "${GREEN}Test 2: Testing idempotency (duplicate requests)...${NC}"
IDEM_KEY="idempotency-test-$(date +%s)"
for i in {1..5}; do
  curl -s -X POST "$BASE_URL/v1/payments/charge" \
    -H "Content-Type: application/json" \
    -H "Idempotency-Key: $IDEM_KEY" \
    -H "X-Correlation-ID: test-idempotency-$i" \
    -d '{
      "amount": 5000,
      "currency": "USD",
      "description": "Idempotency test",
      "customerId": "test-customer",
      "capture": true,
      "paymentMethod": {
        "type": "card",
        "cardNumber": "4242424242424242",
        "expiryMonth": 12,
        "expiryYear": 2025,
        "cvv": "123"
      }
    }' > /dev/null
  echo "  ✓ Request $i/5 sent (same idempotency key)"
done
echo -e "${GREEN}✓ Idempotency test completed${NC}"
echo ""

# Test 3: Get Payment (Rate)
echo -e "${GREEN}Test 3: Fetching payments...${NC}"
for i in {1..20}; do
  # Create a payment first
  PAYMENT_ID=$(curl -s -X POST "$BASE_URL/v1/payments/charge" \
    -H "Content-Type: application/json" \
    -H "Idempotency-Key: fetch-test-$i-$(date +%s)" \
    -d '{
      "amount": 1000,
      "currency": "USD",
      "customerId": "test",
      "capture": false
    }' | jq -r '.id')
  
  # Fetch it
  curl -s "$BASE_URL/v1/payments/$PAYMENT_ID" > /dev/null
  echo "  ✓ Fetched payment $i/20"
done
echo -e "${GREEN}✓ Fetch test completed${NC}"
echo ""

# Test 4: Capture Payments
echo -e "${GREEN}Test 4: Capturing payments...${NC}"
for i in {1..10}; do
  # Create pending payment
  PAYMENT_ID=$(curl -s -X POST "$BASE_URL/v1/payments/charge" \
    -H "Content-Type: application/json" \
    -H "Idempotency-Key: capture-test-$i-$(date +%s)" \
    -d '{
      "amount": 2500,
      "currency": "USD",
      "customerId": "test",
      "capture": false
    }' | jq -r '.id')
  
  # Capture it
  curl -s -X PATCH "$BASE_URL/v1/payments/$PAYMENT_ID/capture" \
    -H "Content-Type: application/json" > /dev/null
  echo "  ✓ Captured payment $i/10"
done
echo -e "${GREEN}✓ Capture test completed${NC}"
echo ""

# Test 5: Cancel Payments
echo -e "${GREEN}Test 5: Canceling payments...${NC}"
for i in {1..10}; do
  # Create pending payment
  PAYMENT_ID=$(curl -s -X POST "$BASE_URL/v1/payments/charge" \
    -H "Content-Type: application/json" \
    -H "Idempotency-Key: cancel-test-$i-$(date +%s)" \
    -d '{
      "amount": 1500,
      "currency": "USD",
      "customerId": "test",
      "capture": false
    }' | jq -r '.id')
  
  # Cancel it
  curl -s -X PATCH "$BASE_URL/v1/payments/$PAYMENT_ID/cancel" \
    -H "Content-Type: application/json" > /dev/null
  echo "  ✓ Canceled payment $i/10"
done
echo -e "${GREEN}✓ Cancel test completed${NC}"
echo ""

# Test 6: Generate Errors (Error Rate)
echo -e "${YELLOW}Test 6: Generating error scenarios...${NC}"
# Invalid payment ID (404 error)
for i in {1..5}; do
  curl -s "$BASE_URL/v1/payments/00000000-0000-0000-0000-000000000000" > /dev/null
  echo "  ✓ Generated 404 error $i/5"
done

# Invalid request body (400 error)
for i in {1..5}; do
  curl -s -X POST "$BASE_URL/v1/payments/charge" \
    -H "Content-Type: application/json" \
    -d '{"invalid": "data"}' > /dev/null
  echo "  ✓ Generated 400 error $i/5"
done
echo -e "${YELLOW}✓ Error scenarios completed${NC}"
echo ""

# Check metrics endpoint
echo -e "${BLUE}📊 Fetching metrics from /metrics endpoint...${NC}"
echo ""
curl -s "$BASE_URL/metrics" | head -50
echo "..."
echo ""

# Summary
echo -e "${BLUE}════════════════════════════════════════════════${NC}"
echo -e "${GREEN}✓ Observability test completed!${NC}"
echo ""
echo "📊 Check metrics at: $BASE_URL/metrics"
echo "🔍 View logs: kubectl logs -l app=payment-api -f"
echo "📈 Grafana queries:"
echo "   - Request rate: rate(http_requests_total[5m])"
echo "   - Error rate: rate(http_errors_total[5m]) / rate(http_requests_total[5m])"
echo "   - P99 latency: histogram_quantile(0.99, rate(http_request_duration_ms_bucket[5m]))"
echo ""
echo "💡 Test Summary:"
echo "   - Payments created: ~$NUM_REQUESTS"
echo "   - Payments captured: 10"
echo "   - Payments canceled: 10"
echo "   - Idempotency tests: 5"
echo "   - Payment fetches: 20"
echo "   - Errors generated: 10 (404 + 400)"
echo ""
