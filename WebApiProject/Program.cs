using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WebApiProject.Data;
using WebApiProject.Filters;
using WebApiProject.Services;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// Configure the HTTP request pipeline - Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API v1");
});

// Add health check endpoint
app.MapGet("/", () => Results.Ok(new { status = "healthy", service = "Payment API", version = "1.0" }))
   .WithName("HealthCheck")
   .Produces<object>(200);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
