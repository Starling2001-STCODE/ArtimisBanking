namespace ArtemisBanking.Application.Common;

public class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }

    public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int pageIndex, int pageSize)
        => new()
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
}