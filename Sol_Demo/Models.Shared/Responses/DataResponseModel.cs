namespace Models.Shared.Responses;

public class DataResponse<TData>
{
    public bool? Success { get; set; }

    public int? StatusCode { get; set; }

    public TData? Data { get; set; }

    public string? Message { get; set; }

    public string? TraceId { get; set; }
}