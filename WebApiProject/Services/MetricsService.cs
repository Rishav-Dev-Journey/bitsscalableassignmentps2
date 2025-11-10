using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WebApiProject.Services;

public class MetricsService
{
  private readonly Meter _meter;

  // RED Metrics (Rate, Errors, Duration)
  private readonly Counter<long> _requestCounter;
  private readonly Counter<long> _errorCounter;
  private readonly Histogram<double> _requestDuration;

  // Business Metrics
  private readonly Counter<long> _paymentsCreatedCounter;
  private readonly Counter<long> _paymentsCapturedCounter;
  private readonly Counter<long> _paymentsCanceledCounter;
  private readonly Counter<long> _paymentsFailedCounter;
  private readonly Histogram<double> _paymentAmountHistogram;
  private readonly Histogram<double> _idempotencyCheckLatency;

  // USE Metrics (Utilization, Saturation, Errors)
  private readonly Counter<long> _databaseConnectionErrors;
  private readonly Histogram<double> _databaseQueryDuration;

  public MetricsService(IMeterFactory meterFactory)
  {
    _meter = meterFactory.Create("PaymentAPI");

    // RED Metrics
    _requestCounter = _meter.CreateCounter<long>(
        "http_requests_total",
        unit: "{requests}",
        description: "Total number of HTTP requests");

    _errorCounter = _meter.CreateCounter<long>(
        "http_errors_total",
        unit: "{errors}",
        description: "Total number of HTTP errors");

    _requestDuration = _meter.CreateHistogram<double>(
        "http_request_duration_ms",
        unit: "ms",
        description: "Duration of HTTP requests in milliseconds");

    // Business Metrics
    _paymentsCreatedCounter = _meter.CreateCounter<long>(
        "payments_created_total",
        unit: "{payments}",
        description: "Total number of payments created");

    _paymentsCapturedCounter = _meter.CreateCounter<long>(
        "payments_captured_total",
        unit: "{payments}",
        description: "Total number of payments captured");

    _paymentsCanceledCounter = _meter.CreateCounter<long>(
        "payments_canceled_total",
        unit: "{payments}",
        description: "Total number of payments canceled");

    _paymentsFailedCounter = _meter.CreateCounter<long>(
        "payments_failed_total",
        unit: "{payments}",
        description: "Total number of failed payment attempts");

    _paymentAmountHistogram = _meter.CreateHistogram<double>(
        "payment_amount_cents",
        unit: "cents",
        description: "Distribution of payment amounts in cents");

    _idempotencyCheckLatency = _meter.CreateHistogram<double>(
        "idempotency_check_latency_ms",
        unit: "ms",
        description: "Time taken to check idempotency");

    // USE Metrics
    _databaseConnectionErrors = _meter.CreateCounter<long>(
        "database_connection_errors_total",
        unit: "{errors}",
        description: "Total number of database connection errors");

    _databaseQueryDuration = _meter.CreateHistogram<double>(
        "database_query_duration_ms",
        unit: "ms",
        description: "Duration of database queries in milliseconds");
  }

  // RED Metrics Methods
  public void RecordRequest(string method, string path, int statusCode, double durationMs)
  {
    var tags = new TagList
        {
            { "method", method },
            { "path", NormalizePath(path) },
            { "status_code", statusCode },
            { "status_category", GetStatusCategory(statusCode) }
        };

    _requestCounter.Add(1, tags);
    _requestDuration.Record(durationMs, tags);

    if (statusCode >= 400)
    {
      _errorCounter.Add(1, tags);
    }
  }

  // Business Metrics Methods
  public void RecordPaymentCreated(long amountCents, string currency, bool captured)
  {
    var tags = new TagList
        {
            { "currency", currency },
            { "captured", captured.ToString().ToLower() }
        };

    _paymentsCreatedCounter.Add(1, tags);
    _paymentAmountHistogram.Record(amountCents, tags);
  }

  public void RecordPaymentCaptured(long amountCents, string currency)
  {
    var tags = new TagList
        {
            { "currency", currency }
        };

    _paymentsCapturedCounter.Add(1, tags);
  }

  public void RecordPaymentCanceled(long amountCents, string currency)
  {
    var tags = new TagList
        {
            { "currency", currency }
        };

    _paymentsCanceledCounter.Add(1, tags);
  }

  public void RecordPaymentFailed(string reason, string currency)
  {
    var tags = new TagList
        {
            { "reason", reason },
            { "currency", currency }
        };

    _paymentsFailedCounter.Add(1, tags);
  }

  public void RecordIdempotencyCheck(double durationMs, bool isIdempotent)
  {
    var tags = new TagList
        {
            { "is_idempotent", isIdempotent.ToString().ToLower() }
        };

    _idempotencyCheckLatency.Record(durationMs, tags);
  }

  // USE Metrics Methods
  public void RecordDatabaseQuery(string queryType, double durationMs, bool success)
  {
    var tags = new TagList
        {
            { "query_type", queryType },
            { "success", success.ToString().ToLower() }
        };

    _databaseQueryDuration.Record(durationMs, tags);

    if (!success)
    {
      _databaseConnectionErrors.Add(1, tags);
    }
  }

  // Helper Methods
  private static string NormalizePath(string path)
  {
    // Normalize paths with IDs to avoid high cardinality
    // e.g., /v1/payments/12345 -> /v1/payments/{id}
    if (path.Contains("/v1/payments/"))
    {
      var parts = path.Split('/');
      if (parts.Length >= 4 && Guid.TryParse(parts[3], out _))
      {
        parts[3] = "{id}";
        return string.Join('/', parts);
      }
    }
    return path;
  }

  private static string GetStatusCategory(int statusCode)
  {
    return statusCode switch
    {
      < 200 => "1xx",
      < 300 => "2xx",
      < 400 => "3xx",
      < 500 => "4xx",
      _ => "5xx"
    };
  }

  // Performance percentile tracking helper
  public IDisposable MeasureOperation(string operationType)
  {
    return new OperationMeasurement(this, operationType);
  }

  private class OperationMeasurement : IDisposable
  {
    private readonly MetricsService _metricsService;
    private readonly string _operationType;
    private readonly Stopwatch _stopwatch;

    public OperationMeasurement(MetricsService metricsService, string operationType)
    {
      _metricsService = metricsService;
      _operationType = operationType;
      _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
      _stopwatch.Stop();
      _metricsService.RecordDatabaseQuery(_operationType, _stopwatch.Elapsed.TotalMilliseconds, true);
    }
  }
}
