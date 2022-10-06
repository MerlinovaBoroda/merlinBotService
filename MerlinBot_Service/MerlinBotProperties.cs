using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;

namespace MerlinBot_Service;

public sealed class MerlinBotProperties : IBotProperties
{
    private readonly BotCommandHelper _commandHelper;

    public MerlinBotProperties(IConfiguration configuration)
    {
        var telegram = configuration.GetSection("Telegram");
        var botToken = telegram["BotToken"];

        Api = new BotClient(botToken);
        User = Api.GetMe();
        
        Console.WriteLine($"{User.Username} started");
        _commandHelper = new BotCommandHelper(this);

        // Delete my old commands
        Api.DeleteMyCommands();
        // Set my commands
        Api.SetMyCommands(
            new BotCommand("hello", "Hello world!"));

        // Delete webhook to use Long Polling
        Api.DeleteWebhook();
    }

    public BotClient Api { get; }
    public User User { get; }

    IBotCommandHelper IBotProperties.CommandHelper => _commandHelper;
}