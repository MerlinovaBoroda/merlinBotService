using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;

namespace MerlinBot_Service.Services;

public partial class MerlinBotService
{
    protected override void OnCommand(Message message, string commandName, string commandParameters)
    {
        var args = commandParameters.Split(' ');
#if DEBUG
        _logger.LogInformation("Params: {0}", args.Length);
#endif

        switch (commandName)
        {
            case "hello": // Reply to /hello command
                var hello = $"Hello World, {message.From!.FirstName}!";
                Api.SendMessage(message.Chat.Id, hello);
                break;
            /*
            case "command1":
                // ...
                break;
            case "command2":
                // ...
                break;
            */
            default:
                if (message.Chat.Type == ChatType.Private)
                {
                    Api.SendMessage(message.Chat.Id, "Unrecognized command.");
                }
                break;
        }
    }
}