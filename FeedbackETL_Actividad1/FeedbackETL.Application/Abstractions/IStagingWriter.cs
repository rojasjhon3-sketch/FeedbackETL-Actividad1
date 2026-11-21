namespace FeedbackETL.Application.Abstractions;

public interface IStagingWriter<T>
{
    Task<long> WriteAsync(
        IAsyncEnumerable<T> records,
        string destinationPath,
        CancellationToken cancellationToken);
}
