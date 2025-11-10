using System.Diagnostics;

namespace WebApiProject.Middleware;

public class MetricsMiddleware
{
  private readonly RequestDelegate _next;
  private readonly Services.MetricsService _metricsService;

  public MetricsMiddleware(RequestDelegate next, Services.MetricsService metricsService)
  {
    _next = next;
    _metricsService = metricsService;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      await _next(context);
    }
    finally
    {
      stopwatch.Stop();

      _metricsService.RecordRequest(
          context.Request.Method,
          context.Request.Path,
          context.Response.StatusCode,
          stopwatch.Elapsed.TotalMilliseconds
      );
    }
  }
}
