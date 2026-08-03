namespace CarePoint.Application.DTOs.Common;

/// <summary>
/// Standard API response wrapper for consistent response format.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public PaginationMetadata? Pagination { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<IReadOnlyList<TItem>> PagedSuccessResponse<TItem>(
        PagedResult<TItem> page, string? message = null) => new()
    {
        Success = true,
        Data = page.Items,
        Message = message,
        Pagination = new PaginationMetadata
        {
            TotalCount = page.TotalCount,
            Skip = page.Skip,
            Take = page.Take,
            HasMore = page.HasMore
        }
    };

    public static ApiResponse<T> FailResponse(string message, List<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors
    };
}

public sealed class PaginationMetadata
{
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public bool HasMore { get; set; }
}
