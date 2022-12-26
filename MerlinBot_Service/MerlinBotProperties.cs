using System.Reflection;
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
        Api.SetMyCommands(new List<BotCommand>()
        {
            new("meaning", "Пояснити англійський термін"),
            new("karma_get", "Переглянути кількість карми"),
            new("karma_top", "Переглянути топ 10 учасників чату по кармі"),
            new("everyone", "Скликати зареєстрованих людей чату"),

            new("byblo", "Гра \"Библо дня\""),
            new("byblo_top", "Переглянути топ гравців у \"Библо дня\""),
            new("byblo_reg", "Зареєструватись в грі \"Библо дня\""),
            new("byblo_rules", "Правила гри \"Библо дня\""),

            new("ctrl_game", "Гра \"Буфер обміну\""),
            new("huyak", "Відпиздити русню"),
            new("joke", "Змусьте бота пожартувати"),
            new("help", "Hmmm...")
        });
        // Delete webhook to use Long Polling
        Api.DeleteWebhook();
    }

    public BotClient Api { get; }
    public User User { get; }

    IBotCommandHelper IBotProperties.CommandHelper => _commandHelper;
}