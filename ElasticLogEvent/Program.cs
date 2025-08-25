using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// A .NET application that logs structured events to a Kafka topic.
/// </summary>
class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((_, services) =>
            {
                services.AddHostedService<Worker>();
            })
            .Build();

        await host.RunAsync();
    }
}

/// <summary>
/// A background service that generates and sends log events to Kafka.
/// </summary>
public class Worker : BackgroundService
{
    /// <summary>
    /// Logger instance for logging information.
    /// </summary>
    private readonly ILogger<Worker> _logger;

    /// <summary>
    /// Kafka producer for sending messages.
    /// </summary>
    private readonly IProducer<Null, string> _producer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = "localhost:9092"
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    /// <summary>
    /// Executes the background service.
    /// </summary>
    /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int counter = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var logEvent = new
            {
                EventType = "JobExecution",
                Service = "KafkaLoggingApp",
                MessageNumber = ++counter,
                DurationSeconds = Math.Round(Random.Shared.NextDouble() * 10, 2),
                Timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(logEvent);

            // Send to Kafka
            await _producer.ProduceAsync(
                "app-logs",
                new Message<Null, string> { Value = json },
                stoppingToken
            );

            // Also log to console
            _logger.LogInformation("Published log: {json}", json);

            await Task.Delay(5000, stoppingToken);
        }
    }
}
