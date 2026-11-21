using FeedbackETL.Application.Abstractions;
using FeedbackETL.Domain.Entities;
using FeedbackETL.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;

namespace FeedbackETL.Worker;

public sealed class Worker : BackgroundService
{
    private readonly IExtractor<SurveyRecord> _csvExtractor;
    private readonly IExtractor<ReviewRecord> _databaseExtractor;
    private readonly IExtractor<ApiComment> _apiExtractor;
    private readonly IStagingWriter<SurveyRecord> _surveyWriter;
    private readonly IStagingWriter<ReviewRecord> _reviewWriter;
    private readonly IStagingWriter<ApiComment> _apiWriter;
    private readonly StagingOptions _stagingOptions;
    private readonly WorkerOptions _workerOptions;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IExtractor<SurveyRecord> csvExtractor,
        IExtractor<ReviewRecord> databaseExtractor,
        IExtractor<ApiComment> apiExtractor,
        IStagingWriter<SurveyRecord> surveyWriter,
        IStagingWriter<ReviewRecord> reviewWriter,
        IStagingWriter<ApiComment> apiWriter,
        IOptions<StagingOptions> stagingOptions,
        IOptions<WorkerOptions> workerOptions,
        ILogger<Worker> logger)
    {
        _csvExtractor = csvExtractor;
        _databaseExtractor = databaseExtractor;
        _apiExtractor = apiExtractor;
        _surveyWriter = surveyWriter;
        _reviewWriter = reviewWriter;
        _apiWriter = apiWriter;
        _stagingOptions = stagingOptions.Value;
        _workerOptions = workerOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_workerOptions.RunIntervalMinutes < 1)
            throw new InvalidOperationException("RunIntervalMinutes debe ser mayor que cero.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            _logger.LogInformation("Inicia ciclo de extracción ETL. RunId: {RunId}", runId);

            var tasks = new[]
            {
                ExecuteSourceAsync("CSV", token =>
                    _surveyWriter.WriteAsync(_csvExtractor.ExtractAsync(token),
                        BuildPath("surveys", runId), token), stoppingToken),

                ExecuteSourceAsync("Base de datos", token =>
                    _reviewWriter.WriteAsync(_databaseExtractor.ExtractAsync(token),
                        BuildPath("reviews", runId), token), stoppingToken),

                ExecuteSourceAsync("API REST", token =>
                    _apiWriter.WriteAsync(_apiExtractor.ExtractAsync(token),
                        BuildPath("api-comments", runId), token), stoppingToken)
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation("Finalizó ciclo de extracción ETL. RunId: {RunId}", runId);

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_workerOptions.RunIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ExecuteSourceAsync(
        string sourceName,
        Func<CancellationToken, Task<long>> action,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var count = await action(cancellationToken);
            stopwatch.Stop();
            _logger.LogInformation(
                "Fuente {Source}: {Count} registros extraídos en {ElapsedMs} ms.",
                sourceName, count, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Extracción cancelada para la fuente {Source}.", sourceName);
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogError(exception,
                "Falló la extracción de {Source} después de {ElapsedMs} ms.",
                sourceName, stopwatch.ElapsedMilliseconds);
        }
    }

    private string BuildPath(string source, string runId) =>
        Path.Combine(_stagingOptions.OutputDirectory, source, $"{source}_{runId}.jsonl");
}
