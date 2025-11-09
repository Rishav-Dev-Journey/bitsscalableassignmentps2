namespace WebApiProject.Models;

public class ChargeRequest
{
  public decimal Amount { get; set; }
  public string Currency { get; set; } = "USD";
  public string? Description { get; set; }
  public string? CustomerId { get; set; }
  public PaymentMethodDetails? PaymentMethod { get; set; }
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
