using Microsoft.EntityFrameworkCore;
using WebApiProject.Data;
using WebApiProject.Models;
using WebApiProject.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register custom services
builder.Services.AddSingleton<IdempotencyService>();
builder.Services.AddScoped<PaymentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// POST /charge - Create (charge) a payment with Idempotency-Key header
app.MapPost("/charge", async (
    ChargeRequest request,
    HttpContext context,
    PaymentService paymentService,
    IdempotencyService idempotencyService) =>
{
    // Check for Idempotency-Key header
    if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) ||
        string.IsNullOrWhiteSpace(idempotencyKey))
    {
        return Results.BadRequest(new { error = "Idempotency-Key header is required" });
    }

    var key = idempotencyKey.ToString();

    // Check if we've already processed this request
    if (idempotencyService.TryGetCachedResponse(key, out var cachedResponse))
    {
        cachedResponse!.IsIdempotent = true;
        return Results.Ok(cachedResponse);
    }

    // Validate request
    if (request.Amount <= 0)
    {
        return Results.BadRequest(new { error = "Amount must be greater than 0" });
    }

    // Process the charge
    var response = await paymentService.ProcessChargeAsync(request);

    // Cache the response for idempotency
    idempotencyService.CacheResponse(key, response);

    return Results.Ok(response);
})
.WithName("ChargePayment")
.WithOpenApi()
.WithDescription("Create a payment charge. Requires Idempotency-Key header to prevent duplicate charges.");

app.Run();
