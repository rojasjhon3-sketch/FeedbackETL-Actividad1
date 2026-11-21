namespace FeedbackETL.Infrastructure.Configuration;

public sealed class CsvOptions
{
    public string FilePath { get; set; } = "Data/encuestas.csv";
}

public sealed class DatabaseOptions
{
    public string Query { get; set; } =
        "SELECT Id, CustomerName, Rating, Comment, CreatedAt FROM dbo.Reviews";
}

public sealed class ApiOptions
{
    public string BaseUrl { get; set; } = "https://jsonplaceholder.typicode.com/";
    public string Endpoint { get; set; } = "comments";
}

public sealed class StagingOptions
{
    public string OutputDirectory { get; set; } = "staging";
}

public sealed class WorkerOptions
{
    public int RunIntervalMinutes { get; set; } = 1440;
}
