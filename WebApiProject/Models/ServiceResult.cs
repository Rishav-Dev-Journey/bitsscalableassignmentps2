namespace WebApiProject.Models;

public class ServiceResult<T>
{
  public bool Success { get; set; }
  public T? Data { get; set; }
  public string? ErrorMessage { get; set; }
  public bool NotFound { get; set; }

  public static ServiceResult<T> SuccessResult(T data)
  {
    return new ServiceResult<T>
    {
      Success = true,
      Data = data
    };
  }

  public static ServiceResult<T> ErrorResult(string errorMessage, bool notFound = false)
  {
    return new ServiceResult<T>
    {
      Success = false,
      ErrorMessage = errorMessage,
      NotFound = notFound
    };
  }
}

public class ChargeOperationResult
{
  public bool Success { get; set; }
  public ChargeResponse? Charge { get; set; }
  public string? ErrorMessage { get; set; }
  public bool NotFound { get; set; }

  public static ChargeOperationResult SuccessResult(ChargeResponse charge)
  {
    return new ChargeOperationResult
    {
      Success = true,
      Charge = charge
    };
  }

  public static ChargeOperationResult ErrorResult(string errorMessage, bool notFound = false)
  {
    return new ChargeOperationResult
    {
      Success = false,
      ErrorMessage = errorMessage,
      NotFound = notFound
    };
  }
}
