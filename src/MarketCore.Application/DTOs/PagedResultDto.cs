namespace MarketCore.Application.DTOs;

public sealed record PagedResultDto<T>
{

    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();

    public int TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public static PagedResultDto<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
        => new() { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
}
