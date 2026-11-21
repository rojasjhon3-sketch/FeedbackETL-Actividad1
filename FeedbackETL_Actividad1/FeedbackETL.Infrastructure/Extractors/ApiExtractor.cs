using FeedbackETL.Application.Abstractions;
using FeedbackETL.Domain.Entities;
using FeedbackETL.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FeedbackETL.Infrastructure.Extractors;

public sealed class ApiExtractor : IExtractor<ApiComment>
{
    private readonly HttpClient _httpClient;
    private readonly ApiOptions _options;
    private readonly ILogger<ApiExtractor> _logger;

    public ApiExtractor(
        HttpClient httpClient,
        IOptions<ApiOptions> options,
        ILogger<ApiExtractor> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<ApiComment> ExtractAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var endpoint = _options.Endpoint.TrimStart('/');
        using var response = await _httpClient.GetAsync(
            endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        await foreach (var comment in JsonSerializer.DeserializeAsyncEnumerable<ApiComment>(
            stream, jsonOptions, cancellationToken))
        {
            if (comment is null)
            {
                _logger.LogWarning("La API devolvió un elemento nulo; se omitirá.");
                continue;
            }

            yield return comment;
        }
    }
}
