using CsvHelper;
using CsvHelper.Configuration;
using FeedbackETL.Application.Abstractions;
using FeedbackETL.Domain.Entities;
using FeedbackETL.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FeedbackETL.Infrastructure.Extractors;

public sealed class CsvExtractor : IExtractor<SurveyRecord>
{
    private readonly CsvOptions _options;
    private readonly ILogger<CsvExtractor> _logger;

    public CsvExtractor(IOptions<CsvOptions> options, ILogger<CsvExtractor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<SurveyRecord> ExtractAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_options.FilePath))
            throw new FileNotFoundException("No se encontró el archivo CSV.", _options.FilePath);

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header.Trim(),
            MissingFieldFound = null,
            HeaderValidated = null
        };

        await using var stream = new FileStream(
            _options.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 64 * 1024, useAsync: true);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, configuration);

        await foreach (var record in csv.GetRecordsAsync<SurveyRecord>(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (record.Rating is < 1 or > 5)
            {
                _logger.LogWarning("Encuesta {Id} omitida: Rating fuera del rango 1-5.", record.Id);
                continue;
            }

            yield return record;
        }
    }
}
