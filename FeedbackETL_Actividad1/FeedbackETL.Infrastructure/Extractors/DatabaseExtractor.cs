using FeedbackETL.Application.Abstractions;
using FeedbackETL.Domain.Entities;
using FeedbackETL.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace FeedbackETL.Infrastructure.Extractors;

public sealed class DatabaseExtractor : IExtractor<ReviewRecord>
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseOptions _options;
    private readonly ILogger<DatabaseExtractor> _logger;

    public DatabaseExtractor(
        IConfiguration configuration,
        IOptions<DatabaseOptions> options,
        ILogger<DatabaseExtractor> logger)
    {
        _configuration = configuration;
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<ReviewRecord> ExtractAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("ReviewsDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Falta configurar ConnectionStrings:ReviewsDb mediante una variable de entorno o User Secrets.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(_options.Query, connection)
        {
            CommandTimeout = 60
        };

        await using var reader = await command.ExecuteReaderAsync(
            System.Data.CommandBehavior.SequentialAccess, cancellationToken);

        var idOrdinal = reader.GetOrdinal("Id");
        var nameOrdinal = reader.GetOrdinal("CustomerName");
        var ratingOrdinal = reader.GetOrdinal("Rating");
        var commentOrdinal = reader.GetOrdinal("Comment");
        var createdOrdinal = reader.GetOrdinal("CreatedAt");

        long extracted = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = new ReviewRecord
            {
                Id = Convert.ToInt32(reader.GetValue(idOrdinal)),
                CustomerName = reader.IsDBNull(nameOrdinal) ? string.Empty : reader.GetString(nameOrdinal),
                Rating = Convert.ToInt32(reader.GetValue(ratingOrdinal)),
                Comment = reader.IsDBNull(commentOrdinal) ? string.Empty : reader.GetString(commentOrdinal),
                CreatedAt = reader.IsDBNull(createdOrdinal) ? DateTime.MinValue : reader.GetDateTime(createdOrdinal)
            };

            if (item.Rating is < 1 or > 5)
            {
                _logger.LogWarning("Reseña {Id} omitida: Rating fuera del rango 1-5.", item.Id);
                continue;
            }

            extracted++;
            yield return item;
        }

        _logger.LogInformation("Consulta de reseñas finalizada. Registros válidos: {Count}", extracted);
    }
}
