using System.Runtime.CompilerServices;

namespace FlightNetwork.DataAccess.DataSeeding;

internal static class AsyncEnumerableExtensions
{
    // Lets an already-materialized in-memory sequence (e.g. RouteDataSeeder's grouped routes)
    // feed into WriteBatchesAsync's IAsyncEnumerable<T> parameter without a second package
    // dependency just for this.
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }

    public static async IAsyncEnumerable<IReadOnlyList<T>> ChunkAsync<T>(
        this IAsyncEnumerable<T> source,
        int size,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);

        var batch = new List<T>(size);

        await foreach (var item in source.WithCancellation(ct))
        {
            batch.Add(item);

            if (batch.Count == size)
            {
                yield return batch;
                batch = new List<T>(size);
            }
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }
}
