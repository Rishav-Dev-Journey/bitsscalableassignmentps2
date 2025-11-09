using Microsoft.AspNetCore.Mvc;
using WebApiProject.Models;
using WebApiProject.Services;

namespace WebApiProject.Controllers;

[ApiController]
[Route("v1/payments")]
public class PaymentsController : ControllerBase
{
  private readonly PaymentService _paymentService;
  private readonly IdempotencyService _idempotencyService;

  public PaymentsController(
      PaymentService paymentService,
      IdempotencyService idempotencyService)
  {
    _paymentService = paymentService;
    _idempotencyService = idempotencyService;
  }

  /// <summary>
  /// Create a payment charge
  /// </summary>
  /// <param name="request">Payment charge request</param>
  /// <returns>Created payment charge</returns>
  [HttpPost("charge")]
  [ProducesResponseType(typeof(ChargeResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> CreateCharge([FromBody] ChargeRequest request)
  {
    // Check for Idempotency-Key header
    if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) ||
        string.IsNullOrWhiteSpace(idempotencyKey))
    {
      return BadRequest(new { error = "Idempotency-Key header is required" });
    }

    var key = idempotencyKey.ToString();

    // Check if we've already processed this request
    if (_idempotencyService.TryGetCachedResponse(key, out var cachedResponse))
    {
      cachedResponse!.IsIdempotent = true;
      return Ok(cachedResponse);
    }

    // Validate request
    if (request.Amount <= 0)
    {
      return BadRequest(new { error = "Amount must be greater than 0" });
    }

    // Process the charge
    var response = await _paymentService.ProcessChargeAsync(request);

    // Cache the response for idempotency
    _idempotencyService.CacheResponse(key, response);

    return Ok(response);
  }

  /// <summary>
  /// Get payment details by ID
  /// </summary>
  /// <param name="payment_id">Payment ID</param>
  /// <returns>Payment details</returns>
  [HttpGet("{payment_id}")]
  [ProducesResponseType(typeof(ChargeResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> GetPayment(string payment_id)
  {
    // Validate payment_id
    if (string.IsNullOrWhiteSpace(payment_id))
    {
      return BadRequest(new { error = "Payment ID is required" });
    }

    var charge = await _paymentService.GetChargeByIdAsync(payment_id);

    if (charge == null)
    {
      return NotFound(new { error = $"Payment with ID '{payment_id}' not found" });
    }

    return Ok(charge);
  }

  /// <summary>
  /// Capture a pending payment
  /// </summary>
  /// <param name="payment_id">Payment ID</param>
  /// <returns>Updated payment details</returns>
  [HttpPatch("{payment_id}/capture")]
  [ProducesResponseType(typeof(ChargeResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> CapturePayment(string payment_id)
  {
    // Validate payment_id
    if (string.IsNullOrWhiteSpace(payment_id))
    {
      return BadRequest(new { error = "Payment ID is required" });
    }

    var result = await _paymentService.CaptureChargeAsync(payment_id);

    if (!result.Success)
    {
      if (result.NotFound)
      {
        return NotFound(new { error = result.ErrorMessage });
      }
      return BadRequest(new { error = result.ErrorMessage });
    }

    return Ok(result.Charge);
  }

  /// <summary>
  /// Cancel a payment
  /// </summary>
  /// <param name="payment_id">Payment ID</param>
  /// <returns>Updated payment details</returns>
  [HttpPatch("{payment_id}/cancel")]
  [ProducesResponseType(typeof(ChargeResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> CancelPayment(string payment_id)
  {
    // Validate payment_id
    if (string.IsNullOrWhiteSpace(payment_id))
    {
      return BadRequest(new { error = "Payment ID is required" });
    }

    var result = await _paymentService.CancelChargeAsync(payment_id);

    if (!result.Success)
    {
      if (result.NotFound)
      {
        return NotFound(new { error = result.ErrorMessage });
      }
      return BadRequest(new { error = result.ErrorMessage });
    }

    return Ok(result.Charge);
  }
}
