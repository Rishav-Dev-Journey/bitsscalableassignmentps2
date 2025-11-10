namespace WebApiProject.Models;

public class ChargeRequest
{
  public long Amount { get; set; } // Amount in cents (e.g., 1000 = $10.00)
  public string Currency { get; set; } = "USD";
  public string? Description { get; set; }
  public string? CustomerId { get; set; }
  public PaymentMethodDetails? PaymentMethod { get; set; }
  public bool? Capture { get; set; } = true; // If false, payment will be in "pending" status
}

public class PaymentMethodDetails
{
  public string Type { get; set; } = "card";
  public string? CardNumber { get; set; }
  public string? CardholderName { get; set; }
  public int? ExpiryMonth { get; set; }
  public int? ExpiryYear { get; set; }
  public string? Cvv { get; set; }
}
