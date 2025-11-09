using Microsoft.EntityFrameworkCore;
using WebApiProject.Data;
using WebApiProject.Models;

namespace WebApiProject.Services;

public class PaymentService
{
  private readonly PaymentDbContext _dbContext;

  public PaymentService(PaymentDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<ChargeResponse> ProcessChargeAsync(ChargeRequest request)
  {
    // Simulate payment processing
    await Task.Delay(100); // Simulate API call

    var chargeId = $"ch_{Guid.NewGuid().ToString("N")[..24]}";
    var createdAt = DateTime.UtcNow;

    // Determine initial status based on capture setting
    // If capture is true or not specified, status is "succeeded" and ready to be captured
    // If capture is false, status is "pending" and needs manual capture
    var status = request.Capture.HasValue && !request.Capture.Value ? "pending" : "succeeded";

    // Save charge to database
    var charge = new Charge
    {
      Id = chargeId,
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

    _dbContext.Charges.Add(charge);
    await _dbContext.SaveChangesAsync();

    return new ChargeResponse
    {
      Id = chargeId,
      Status = status,
      Amount = request.Amount,
      Currency = request.Currency,
      Description = request.Description,
      CustomerId = request.CustomerId,
      CreatedAt = createdAt,
      IsIdempotent = false
    };
  }

  public async Task<ChargeResponse?> GetChargeByIdAsync(string paymentId)
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

  public async Task<ChargeOperationResult> CaptureChargeAsync(string paymentId)
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
      return ChargeOperationResult.ErrorResult("Cannot capture a canceled payment");
    }

    if (charge.Status != "pending" && charge.Status != "succeeded")
    {
      return ChargeOperationResult.ErrorResult(
          $"Cannot capture payment with status '{charge.Status}'");
    }

    // Update charge status to captured
    charge.Status = "captured";
    await _dbContext.SaveChangesAsync();

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

  public async Task<ChargeOperationResult> CancelChargeAsync(string paymentId)
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
      return ChargeOperationResult.ErrorResult(
          "Cannot cancel a captured payment. Please use refund instead.");
    }

    if (charge.Status == "refunded")
    {
      return ChargeOperationResult.ErrorResult("Cannot cancel a refunded payment");
    }

    // Update charge status to canceled
    charge.Status = "canceled";
    await _dbContext.SaveChangesAsync();

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
