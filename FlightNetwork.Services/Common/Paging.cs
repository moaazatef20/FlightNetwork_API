namespace FlightNetwork.Services.Common;

internal static class Paging
{
    /// <summary>Hard ceiling on page size so a caller can't force an unbounded read off the graph.</summary>
    public const int MaxPageSize = 100;

    public static (int Skip, int Limit) ToSkipLimit(int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaxPageSize);

        return ((page - 1) * pageSize, pageSize);
    }
}
