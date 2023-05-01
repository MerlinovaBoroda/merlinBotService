using MerlinBot_Service.Services;
using Telegram.BotAPI;
using Telegram.BotAPI.GettingUpdates;

namespace MerlinBot_Service;

public class Worker : BackgroundService
{
    private readonly BotClient _api;
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MerlinBotProperties _botProperties;

    public Worker(ILogger<Worker> logger, MerlinBotProperties botProperties, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _botProperties = botProperties;
        _api = botProperties.Api;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);

        // Long Polling
        var updates = await _api.GetUpdatesAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
        var retryCount = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (updates.Any())
                {
                    Parallel.ForEach(updates, (update) => ProcessUpdate(update));

                    updates = await _api.GetUpdatesAsync(updates[^1].UpdateId + 1, cancellationToken: stoppingToken).ConfigureAwait(false);
                    retryCount = 0;
                }
                else
                {
                    updates = await _api.GetUpdatesAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the background service.");

                // increment retry count
                retryCount++;

                // check if we've exceeded the maximum number of retries
                if (retryCount > _botProperties.BackgroundServiceMaxRetries)
                {
                    _logger.LogError($"Exceeded maximum number of retries ({_botProperties.BackgroundServiceMaxRetries}). Stopping background service.");
                    break;
                }

                // add delay before retrying
                await Task.Delay(_botProperties.BackgroundServiceRetryDelay, stoppingToken);
            }
        }
    }

    private void ProcessUpdate(Update update)
    {
        using var scope = _serviceProvider.CreateScope();
        var bot = scope.ServiceProvider.GetRequiredService<MerlinBotService>();
        bot.OnUpdate(update);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker stopping at: {Time}", DateTimeOffset.Now);
        return base.StopAsync(cancellationToken);
    }
}