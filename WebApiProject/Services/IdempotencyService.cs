using System.Collections.Concurrent;
using WebApiProject.Models;

namespace WebApiProject.Services;

public class IdempotencyService
{
  private readonly ConcurrentDictionary<string, ChargeResponse> _cache = new();

  public bool TryGetCachedResponse(string idempotencyKey, out ChargeResponse? response)
  {
    return _cache.TryGetValue(idempotencyKey, out response);
  }

  public void CacheResponse(string idempotencyKey, ChargeResponse response)
  {
    _cache.TryAdd(idempotencyKey, response);
  }
}
