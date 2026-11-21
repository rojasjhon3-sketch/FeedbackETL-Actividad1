using FeedbackETL.Application.Abstractions;
using FeedbackETL.Domain.Entities;
using FeedbackETL.Infrastructure.Configuration;
using FeedbackETL.Infrastructure.Extractors;
using FeedbackETL.Infrastructure.Staging;
using Microsoft.Extensions.Options;
using FeedbackETL.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<CsvOptions>(
    builder.Configuration.GetSection("Extraction:Csv"));
builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection("Extraction:Database"));
builder.Services.Configure<ApiOptions>(
    builder.Configuration.GetSection("Extraction:Api"));
builder.Services.Configure<StagingOptions>(
    builder.Configuration.GetSection("Staging"));
builder.Services.Configure<WorkerOptions>(
    builder.Configuration.GetSection("Worker"));

builder.Services.AddTransient<IExtractor<SurveyRecord>, CsvExtractor>();
builder.Services.AddTransient<IExtractor<ReviewRecord>, DatabaseExtractor>();

builder.Services.AddHttpClient<ApiExtractor>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FeedbackETL/1.0");
});
builder.Services.AddTransient<IExtractor<ApiComment>>(
    services => services.GetRequiredService<ApiExtractor>());

builder.Services.AddSingleton(typeof(IStagingWriter<>), typeof(JsonLinesStagingWriter<>));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
