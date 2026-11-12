namespace PortfolioAPI.Core.Common
{
    /// <summary>
    /// Server-side pagination envelope (issue #132). The repository executes
    /// the COUNT(*) and the page query in SQL — no full-table materialization
    /// — and the controller exposes <c>Items</c> plus <see cref="TotalCount"/>
    /// as the X-Total-Count response header.
    /// </summary>
    public sealed record PagedResult<T>(
        IReadOnlyList<T> Items,
        int TotalCount,
        int Page,
        int PageSize)
    {
        public int TotalPages => PageSize > 0
            ? (int)Math.Ceiling(TotalCount / (double)PageSize)
            : 0;
    }
}
