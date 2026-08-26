using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FlightNetwork.DataAccess.DataSeeding;


internal sealed class SeedFileReader(IOptions<SeedingOptions> options)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {

        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filesDirectory = ResolveDirectory(options.Value.FilesDirectory);

    public bool FileExists(string fileName) => File.Exists(Path.Combine(_filesDirectory, fileName));

    public async IAsyncEnumerable<T> ReadAsync<T>(
        string fileName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var path = Path.Combine(_filesDirectory, fileName);

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(stream, SerializerOptions, ct))
        {
            if (item is not null)
            {
                yield return item;
            }
        }
    }

    private static string ResolveDirectory(string configured) =>
        Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);
}
