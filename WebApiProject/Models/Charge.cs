namespace WebApiProject.Models;

public class Charge
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public string Status { get; set; } = string.Empty;
  public long Amount { get; set; }
  public string Currency { get; set; } = string.Empty;
  public string? Description { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public string? CustomerId { get; set; }
  public string? PaymentMethodType { get; set; }
  public string? CardLast4 { get; set; }
}
