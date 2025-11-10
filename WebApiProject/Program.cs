using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using WebApiProject.Data;
using WebApiProject.Filters;
using WebApiProject.Middleware;
using WebApiProject.Services;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "PaymentAPI")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/payment-api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 100_000_000,
        rollOnFileSizeLimit: true)
    .CreateLogger();

try
{
    Log.Information("Starting Payment API application");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Payment API", Version = "v1" });

        // Include XML comments for Swagger documentation
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }

        // Add Idempotency-Key header parameter for all operations
        c.OperationFilter<IdempotencyKeyHeaderOperationFilter>();
    });

    // Add Database
    builder.Services.AddDbContext<PaymentDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Register custom services
    builder.Services.AddSingleton<IdempotencyService>();
    builder.Services.AddScoped<PaymentService>();
    builder.Services.AddSingleton<MetricsService>();

    // Add OpenTelemetry with Prometheus exporter
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService("PaymentAPI")
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production",
                ["service.version"] = "1.3"
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("PaymentAPI")
            .AddPrometheusExporter())
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, httpRequest) =>
                {
                    activity.SetTag("correlation_id", httpRequest.HttpContext.Items["CorrelationId"]);
                };
                options.EnrichWithHttpResponse = (activity, httpResponse) =>
                {
                    activity.SetTag("response.status_code", httpResponse.StatusCode);
                };
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.RecordException = true;
            }));

    var app = builder.Build();

    // Configure the HTTP request pipeline

    // Add custom middleware
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<MetricsMiddleware>();

    // Enable Swagger in all environments
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API v1");
    });

    // Add health check endpoint
    app.MapGet("/", () => Results.Ok(new
    {
        status = "healthy",
        service = "Payment API",
        version = "1.3",
        timestamp = DateTime.UtcNow.ToString("o")
    }))
       .WithName("HealthCheck")
       .Produces<object>(200);

    // Prometheus metrics endpoint
    app.MapPrometheusScrapingEndpoint("/metrics");

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    Log.Information("Payment API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
