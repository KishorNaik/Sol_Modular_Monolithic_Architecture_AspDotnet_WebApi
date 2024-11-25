namespace Models.Shared.Responses;

public class PaginationDataResponseModel
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

public class PaginationDataResponse<TData> : DataResponse<TData>
{
    public PaginationDataResponseModel? Pagination { get; set; }
}

public class PaginationDataResult<TData> : PaginationDataResponse<TData>
{
}