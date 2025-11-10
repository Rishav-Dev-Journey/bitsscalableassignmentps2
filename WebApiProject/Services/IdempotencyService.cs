using System.Collections.Concurrent;
using System.Diagnostics;
using WebApiProject.Models;

namespace WebApiProject.Services;

public class IdempotencyService
{
  private readonly ConcurrentDictionary<string, ChargeResponse> _cache = new();
  private readonly MetricsService _metricsService;
  private readonly ILogger<IdempotencyService> _logger;

  public IdempotencyService(MetricsService metricsService, ILogger<IdempotencyService> logger)
  {
    _metricsService = metricsService;
    _logger = logger;
  }

  public bool TryGetCachedResponse(string idempotencyKey, out ChargeResponse? response)
  {
    var stopwatch = Stopwatch.StartNew();
    var found = _cache.TryGetValue(idempotencyKey, out response);
    stopwatch.Stop();

    _metricsService.RecordIdempotencyCheck(stopwatch.Elapsed.TotalMilliseconds, found);

    if (found)
    {
      _logger.LogInformation(
        "Idempotency key found in cache: {IdempotencyKey}, PaymentId: {PaymentId}",
        idempotencyKey, response?.Id);
    }

    return found;
  }

  public void CacheResponse(string idempotencyKey, ChargeResponse response)
  {
    var added = _cache.TryAdd(idempotencyKey, response);

    if (added)
    {
      _logger.LogInformation(
        "Cached response for idempotency key: {IdempotencyKey}, PaymentId: {PaymentId}",
        idempotencyKey, response.Id);
    }
  }
}
