using Telegram.BotAPI;

namespace MerlinBot_Service.Services;

public partial class MerlinBotService
{
    protected override void OnBotException(BotRequestException exp)
    {
        _logger.LogError("BotRequestException: {Message}", exp.Message);
    }

    protected override void OnException(Exception exp)
    {
        _logger.LogError("Exception: {Message}", exp.Message);
    }
}