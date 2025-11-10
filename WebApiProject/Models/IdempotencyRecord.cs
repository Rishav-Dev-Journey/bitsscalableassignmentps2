namespace WebApiProject.Models;

public class IdempotencyRecord
{
  public int Id { get; set; }
  public string IdempotencyKey { get; set; } = string.Empty;
  public string ChargeId { get; set; } = string.Empty;
  public string ResponseData { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
