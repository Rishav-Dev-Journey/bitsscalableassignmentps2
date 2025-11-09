using WebApiProject.Models;

namespace WebApiProject.Services;

public class PaymentService
{
  public async Task<ChargeResponse> ProcessChargeAsync(ChargeRequest request)
  {
    // Simulate payment processing
    await Task.Delay(100); // Simulate API call

    var chargeId = $"ch_{Guid.NewGuid().ToString("N")[..24]}";

    return new ChargeResponse
    {
      Id = chargeId,
      Status = "succeeded",
      Amount = request.Amount,
      Currency = request.Currency,
      Description = request.Description,
      CustomerId = request.CustomerId,
      CreatedAt = DateTime.UtcNow,
      IsIdempotent = false
    };
  }
}
