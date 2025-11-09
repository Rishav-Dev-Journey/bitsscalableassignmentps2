namespace WebApiProject.Models;

public class IdempotencyRecord
{
  public string IdempotencyKey { get; set; } = string.Empty;
  public string ChargeId { get; set; } = string.Empty;
  public string ResponseData { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
}
