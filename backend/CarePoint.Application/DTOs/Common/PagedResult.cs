namespace CarePoint.Application.DTOs.Common;

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Skip { get; init; }
    public required int Take { get; init; }

    public bool HasMore => Skip + Items.Count < TotalCount;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int skip, int take) => new()
    {
        Items = items,
        TotalCount = totalCount,
        Skip = skip,
        Take = take
    };
}

public static class Pagination
{
    public static (int Skip, int Take) Normalize(int skip, int take) =>
        (Math.Max(0, skip), Math.Clamp(take, 1, 100));
}
