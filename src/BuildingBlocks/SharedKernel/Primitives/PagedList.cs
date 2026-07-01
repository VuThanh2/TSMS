namespace SharedKernel.Primitives;

/// Wraps a paginated subset of data along with metadata about the full result set.
/// Used by Query handlers to return paged results to the Presentation layer.
public sealed class PagedList<T> {
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    private PagedList(IReadOnlyList<T> items, int page, int pageSize, int totalCount) {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedList<T> Create(IEnumerable<T> source, int page, int pageSize) {
        var items = source.ToList();
        var totalCount = items.Count;
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<T>(paged, page, pageSize, totalCount);
    }

    /// Use this overload when the caller already has the total count from a database COUNT query,
    /// and source contains only the current page's items (already sliced at DB level).
    public static PagedList<T> Create(IEnumerable<T> pagedSource, int page, int pageSize, int totalCount) {
        return new PagedList<T>(pagedSource.ToList(), page, pageSize, totalCount);
    }
}