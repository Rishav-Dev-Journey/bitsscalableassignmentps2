using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebApiProject.Middleware;

public class RequestLoggingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<RequestLoggingMiddleware> _logger;
  private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
  private static readonly Regex PhoneRegex = new(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);
  private static readonly Regex CardNumberRegex = new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled);

  public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                       ?? Guid.NewGuid().ToString();
    var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Items["TraceId"] = traceId;
        
        // Add correlation ID to response headers
        context.Response.Headers.Append("X-Correlation-ID", correlationId);
        context.Response.Headers.Append("X-Trace-ID", traceId);    var stopwatch = Stopwatch.StartNew();
    var requestBody = await ReadRequestBodyAsync(context.Request);

    // Store original response body stream
    var originalBodyStream = context.Response.Body;

    try
    {
      using var responseBody = new MemoryStream();
      context.Response.Body = responseBody;

      await _next(context);

      stopwatch.Stop();

      var responseBodyText = await ReadResponseBodyAsync(context.Response);

      // Log structured request/response
      LogRequest(context, correlationId, traceId, stopwatch.ElapsedMilliseconds,
                requestBody, responseBodyText, context.Response.StatusCode);

      await responseBody.CopyToAsync(originalBodyStream);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      LogError(context, correlationId, traceId, stopwatch.ElapsedMilliseconds, ex);
      throw;
    }
    finally
    {
      context.Response.Body = originalBodyStream;
    }
  }

  private async Task<string> ReadRequestBodyAsync(HttpRequest request)
  {
    if (!request.ContentLength.HasValue || request.ContentLength == 0)
      return string.Empty;

    request.EnableBuffering();

    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
    var body = await reader.ReadToEndAsync();
    request.Body.Position = 0;

    return MaskSensitiveData(body);
  }

  private async Task<string> ReadResponseBodyAsync(HttpResponse response)
  {
    response.Body.Seek(0, SeekOrigin.Begin);
    var text = await new StreamReader(response.Body).ReadToEndAsync();
    response.Body.Seek(0, SeekOrigin.Begin);

    return MaskSensitiveData(text);
  }

  private string MaskSensitiveData(string data)
  {
    if (string.IsNullOrEmpty(data))
      return data;

    // Mask email addresses
    data = EmailRegex.Replace(data, m => MaskEmail(m.Value));

    // Mask phone numbers
    data = PhoneRegex.Replace(data, "***-***-****");

    // Mask card numbers (keep last 4 digits)
    data = CardNumberRegex.Replace(data, m => MaskCardNumber(m.Value));

    // Mask CVV
    data = Regex.Replace(data, @"""cvv""\s*:\s*""?\d{3,4}""?", @"""cvv"":""***""", RegexOptions.IgnoreCase);

    // Mask passwords
    data = Regex.Replace(data, @"""password""\s*:\s*""[^""]*""", @"""password"":""***""", RegexOptions.IgnoreCase);

    return data;
  }

  private string MaskEmail(string email)
  {
    var parts = email.Split('@');
    if (parts.Length != 2) return "***@***.com";

    var localPart = parts[0];
    var maskedLocal = localPart.Length > 2
        ? $"{localPart[0]}***{localPart[^1]}"
        : "***";

    return $"{maskedLocal}@{parts[1]}";
  }

  private string MaskCardNumber(string cardNumber)
  {
    var digits = Regex.Replace(cardNumber, @"[\s-]", "");
    if (digits.Length < 4) return "****";

    var last4 = digits[^4..];
    return $"****-****-****-{last4}";
  }

  private void LogRequest(HttpContext context, string correlationId, string traceId,
                         long durationMs, string requestBody, string responseBody, int statusCode)
  {
    var logData = new
    {
      Timestamp = DateTime.UtcNow.ToString("o"),
      CorrelationId = correlationId,
      TraceId = traceId,
      Request = new
      {
        Method = context.Request.Method,
        Path = context.Request.Path.Value,
        QueryString = context.Request.QueryString.Value,
        Headers = context.Request.Headers
                .Where(h => !h.Key.Contains("Authorization", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => h.Value.ToString()),
        Body = requestBody,
        ClientIp = context.Connection.RemoteIpAddress?.ToString()
      },
      Response = new
      {
        StatusCode = statusCode,
        Body = responseBody.Length > 5000 ? $"{responseBody[..5000]}... (truncated)" : responseBody
      },
      Performance = new
      {
        DurationMs = durationMs,
        DurationCategory = GetDurationCategory(durationMs)
      },
      User = new
      {
        IsAuthenticated = context.User.Identity?.IsAuthenticated ?? false,
        Name = context.User.Identity?.Name ?? "anonymous"
      }
    };

    var level = statusCode >= 500 ? LogLevel.Error
              : statusCode >= 400 ? LogLevel.Warning
              : durationMs > 5000 ? LogLevel.Warning
              : LogLevel.Information;

    _logger.Log(level, "HTTP {Method} {Path} completed with {StatusCode} in {DurationMs}ms | CorrelationId: {CorrelationId} | TraceId: {TraceId}",
        context.Request.Method,
        context.Request.Path,
        statusCode,
        durationMs,
        correlationId,
        traceId);

    // Log full structured data as JSON
    _logger.LogInformation("REQUEST_DETAILS: {LogData}", JsonSerializer.Serialize(logData));
  }

  private void LogError(HttpContext context, string correlationId, string traceId,
                       long durationMs, Exception ex)
  {
    var errorData = new
    {
      Timestamp = DateTime.UtcNow.ToString("o"),
      CorrelationId = correlationId,
      TraceId = traceId,
      Request = new
      {
        Method = context.Request.Method,
        Path = context.Request.Path.Value,
        QueryString = context.Request.QueryString.Value
      },
      Error = new
      {
        Type = ex.GetType().Name,
        Message = ex.Message,
        StackTrace = ex.StackTrace,
        InnerException = ex.InnerException?.Message
      },
      Performance = new
      {
        DurationMs = durationMs
      }
    };

    _logger.LogError(ex, "HTTP {Method} {Path} failed after {DurationMs}ms | CorrelationId: {CorrelationId} | TraceId: {TraceId} | Error: {ErrorMessage}",
        context.Request.Method,
        context.Request.Path,
        durationMs,
        correlationId,
        traceId,
        ex.Message);

    _logger.LogError("ERROR_DETAILS: {ErrorData}", JsonSerializer.Serialize(errorData));
  }

  private static string GetDurationCategory(long durationMs)
  {
    return durationMs switch
    {
      < 100 => "fast",
      < 500 => "normal",
      < 1000 => "slow",
      < 5000 => "very_slow",
      _ => "critical"
    };
  }
}
