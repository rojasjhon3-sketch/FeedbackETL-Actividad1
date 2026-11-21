namespace FeedbackETL.Application.Abstractions;

public interface IExtractor<T>
{
    IAsyncEnumerable<T> ExtractAsync(CancellationToken cancellationToken);
}
