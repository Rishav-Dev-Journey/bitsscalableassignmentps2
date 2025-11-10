using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApiProject.Data;
using WebApiProject.Models;

namespace WebApiProject.Services;

public class PaymentService
{
  private readonly PaymentDbContext _dbContext;
  private readonly MetricsService _metricsService;
  private readonly ILogger<PaymentService> _logger;

  public PaymentService(
    PaymentDbContext dbContext,
    MetricsService metricsService,
    ILogger<PaymentService> logger)
  {
    _dbContext = dbContext;
    _metricsService = metricsService;
    _logger = logger;
  }

  public async Task<ChargeResponse> ProcessChargeAsync(ChargeRequest request)
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      // Simulate payment processing
      await Task.Delay(100); // Simulate API call

      var createdAt = DateTime.UtcNow;

      // Determine initial status based on capture setting
      var status = request.Capture.HasValue && !request.Capture.Value ? "pending" : "succeeded";
      var isCaptured = status == "succeeded";

      // Save charge to database
      var charge = new Charge
      {
        Id = Guid.NewGuid(),
        Status = status,
        Amount = request.Amount,
        Currency = request.Currency,
        Description = request.Description,
        CustomerId = request.CustomerId,
        CreatedAt = createdAt,
        PaymentMethodType = request.PaymentMethod?.Type,
        CardLast4 = request.PaymentMethod?.CardNumber?.Length >= 4
          ? request.PaymentMethod.CardNumber[^4..]
          : null
      };

      using (_metricsService.MeasureOperation("insert_charge"))
      {
        _dbContext.Charges.Add(charge);
        await _dbContext.SaveChangesAsync();
      }

      // Record business metrics
      _metricsService.RecordPaymentCreated(request.Amount, request.Currency, isCaptured);

      _logger.LogInformation(
        "Payment created: {PaymentId}, Amount: {Amount} {Currency}, Status: {Status}, Customer: {CustomerId}",
        charge.Id, request.Amount, request.Currency, status, request.CustomerId);

      stopwatch.Stop();

      return new ChargeResponse
      {
        Id = charge.Id,
        Status = status,
        Amount = request.Amount,
        Currency = request.Currency,
        Description = request.Description,
        CustomerId = request.CustomerId,
        CreatedAt = createdAt,
        IsIdempotent = false
      };
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      _metricsService.RecordPaymentFailed("processing_error", request.Currency);
      _logger.LogError(ex, "Failed to process payment: {Amount} {Currency}", request.Amount, request.Currency);
      throw;
    }
  }

  public async Task<ChargeResponse?> GetChargeByIdAsync(Guid paymentId)
  {
    using (_metricsService.MeasureOperation("select_charge_by_id"))
    {
      var charge = await _dbContext.Charges
          .FirstOrDefaultAsync(c => c.Id == paymentId);

      if (charge == null)
      {
        return null;
      }

      return new ChargeResponse
      {
        Id = charge.Id,
        Status = charge.Status,
        Amount = charge.Amount,
        Currency = charge.Currency,
        Description = charge.Description,
        CustomerId = charge.CustomerId,
        CreatedAt = charge.CreatedAt,
        IsIdempotent = false
      };
    }
  }

  public async Task<ChargeOperationResult> CaptureChargeAsync(Guid paymentId)
  {
    var charge = await _dbContext.Charges
        .FirstOrDefaultAsync(c => c.Id == paymentId);

    if (charge == null)
    {
      return ChargeOperationResult.ErrorResult(
          $"Payment with ID '{paymentId}' not found",
          notFound: true);
    }

    // Check if payment can be captured
    if (charge.Status == "captured")
    {
      return ChargeOperationResult.ErrorResult("Payment has already been captured");
    }

    if (charge.Status == "canceled")
    {
      _metricsService.RecordPaymentFailed("already_canceled", charge.Currency);
      return ChargeOperationResult.ErrorResult("Cannot capture a canceled payment");
    }

    if (charge.Status != "pending" && charge.Status != "succeeded")
    {
      _metricsService.RecordPaymentFailed("invalid_status", charge.Currency);
      return ChargeOperationResult.ErrorResult(
          $"Cannot capture payment with status '{charge.Status}'");
    }

    // Update charge status to captured
    charge.Status = "captured";

    using (_metricsService.MeasureOperation("update_charge_capture"))
    {
      await _dbContext.SaveChangesAsync();
    }

    // Record business metrics
    _metricsService.RecordPaymentCaptured(charge.Amount, charge.Currency);

    _logger.LogInformation(
      "Payment captured: {PaymentId}, Amount: {Amount} {Currency}",
      charge.Id, charge.Amount, charge.Currency);

    var response = new ChargeResponse
    {
      Id = charge.Id,
      Status = charge.Status,
      Amount = charge.Amount,
      Currency = charge.Currency,
      Description = charge.Description,
      CustomerId = charge.CustomerId,
      CreatedAt = charge.CreatedAt,
      IsIdempotent = false
    };

    return ChargeOperationResult.SuccessResult(response);
  }

  public async Task<ChargeOperationResult> CancelChargeAsync(Guid paymentId)
  {
    var charge = await _dbContext.Charges
        .FirstOrDefaultAsync(c => c.Id == paymentId);

    if (charge == null)
    {
      return ChargeOperationResult.ErrorResult(
          $"Payment with ID '{paymentId}' not found",
          notFound: true);
    }

    // Check if payment can be canceled
    if (charge.Status == "canceled")
    {
      return ChargeOperationResult.ErrorResult("Payment has already been canceled");
    }

    if (charge.Status == "captured")
    {
      _metricsService.RecordPaymentFailed("already_captured", charge.Currency);
      return ChargeOperationResult.ErrorResult(
          "Cannot cancel a captured payment. Please use refund instead.");
    }

    if (charge.Status == "refunded")
    {
      return ChargeOperationResult.ErrorResult("Cannot cancel a refunded payment");
    }

    // Update charge status to canceled
    charge.Status = "canceled";

    using (_metricsService.MeasureOperation("update_charge_cancel"))
    {
      await _dbContext.SaveChangesAsync();
    }

    // Record business metrics
    _metricsService.RecordPaymentCanceled(charge.Amount, charge.Currency);

    _logger.LogInformation(
      "Payment canceled: {PaymentId}, Amount: {Amount} {Currency}",
      charge.Id, charge.Amount, charge.Currency);

    var response = new ChargeResponse
    {
      Id = charge.Id,
      Status = charge.Status,
      Amount = charge.Amount,
      Currency = charge.Currency,
      Description = charge.Description,
      CustomerId = charge.CustomerId,
      CreatedAt = charge.CreatedAt,
      IsIdempotent = false
    };

    return ChargeOperationResult.SuccessResult(response);
  }
}
