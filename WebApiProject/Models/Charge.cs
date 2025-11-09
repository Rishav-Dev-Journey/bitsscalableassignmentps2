namespace WebApiProject.Models;

public class Charge
{
  public string Id { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public string Currency { get; set; } = string.Empty;
  public string? Description { get; set; }
  public DateTime CreatedAt { get; set; }
  public string? CustomerId { get; set; }
  public string? PaymentMethodType { get; set; }
  public string? CardLast4 { get; set; }
}
