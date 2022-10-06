using Telegram.BotAPI;

namespace MerlinBot_Service.Services;

public partial class MerlinBotService : TelegramBotBase<MerlinBotProperties>
{
    private readonly ILogger<MerlinBotService> _logger;
    
    public MerlinBotService(MerlinBotProperties botProperties, ILogger<MerlinBotService> logger) : base(botProperties)
    {
        _logger = logger;
    }
}