using FeedbackETL.Application.Abstractions;
using System.Text.Json;

namespace FeedbackETL.Infrastructure.Staging;

public sealed class JsonLinesStagingWriter<T> : IStagingWriter<T>
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<long> WriteAsync(
        IAsyncEnumerable<T> records,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        long count = 0;

        await using var stream = new FileStream(
            destinationPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 64 * 1024, useAsync: true);
        await using var writer = new StreamWriter(stream);

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            var json = JsonSerializer.Serialize(record, JsonOptions);
            await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
            count++;
        }

        await writer.FlushAsync(cancellationToken);
        return count;
    }
}
