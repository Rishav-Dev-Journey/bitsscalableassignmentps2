namespace WebApiProject.Models;

public class ChargeResponse
{
  public Guid Id { get; set; }
  public string Status { get; set; } = string.Empty;
  public long Amount { get; set; }
  public string Currency { get; set; } = string.Empty;
  public string? Description { get; set; }
  public DateTime CreatedAt { get; set; }
  public string? CustomerId { get; set; }
  public bool IsIdempotent { get; set; }
}
