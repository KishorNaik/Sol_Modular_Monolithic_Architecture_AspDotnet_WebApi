namespace Models.Shared.Responses;

public class ErrorHandlerModel
{
    public ErrorHandlerModel(bool success, int statusCode, string message, string traceId)
    {
        this.Message = message;
        this.Success = success;
        this.StatusCode = statusCode;
        this.TraceId = traceId;
    }

    public bool Success { get; }

    public int StatusCode { get; }

    public string Message { get; }

    public string TraceId { get; }
}