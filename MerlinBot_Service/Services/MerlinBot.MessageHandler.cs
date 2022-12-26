using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using static MerlinBot_Service.Stuff.Messages;
using static MerlinBot_Service.Stuff.Gifs;

namespace MerlinBot_Service.Services;

public partial class MerlinBotService
{
    protected override async void OnMessage(Message message)
    {
        // Ignore user 777000 (Telegram)
        if (message.From?.Id == TelegramConstants.TelegramId)
        {
            return;
        }

        var appUser = message.From; // Save current user;
        var hasText = !string.IsNullOrEmpty(message.Text); // True if message has text;

#if DEBUG
        _logger.LogInformation("New message from chat id: {ChatId}", message.Chat.Id);
        _logger.LogInformation("Message: {MessageContent}", hasText ? message.Text : "No text");
#endif

        // Only private Chats
        if (message.Chat.Type == ChatType.Private)
        {
            Helpers.SavePrivateChat(appUser);
        }
        else // Only group chats
        {
            //Save chat and user tu database
            Helpers.SaveChat(message);

            if (message.LeftChatMember != null)
            {
                var leftUser = message.LeftChatMember;
                if (leftUser.IsBot) return;

                Helpers.RemoveUserFromDatabases(message, leftUser);

                var random = new Random();
                await Api.SendMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"@{leftUser.Username}{LeftMessages[random.Next(LeftMessages.Length)]}"
                );
            }

            //Karma count
            //Check if message is reply 
            if (message.ReplyToMessage != null)
            {
                //Check if message is sticker
                if (message.Sticker != null)
                {
                    //Check if Sticker emoji Contains like or dislike
                    if (message.Sticker.Emoji.Contains("👍") || message.Sticker.Emoji.Contains("👎"))
                    {
                        if (!Helpers.CheckKarmaMessage(Api, message))
                        {
                            return;
                        }

                        //Add karma
                        if (message.Sticker!.Emoji.Contains("👍"))
                        {
                            Helpers.SendPlusKarma(Api, message);
                        }

                        //Subtract karma
                        if (message.Sticker.Emoji.Contains("👎"))
                        {
                            Helpers.SendMinusKarma(Api, message);
                        }
                    }

                    return;
                }

                //Check if message starts with + or -
                if (message.Text!.StartsWith("+") || message.Text.StartsWith("-"))
                {
                    if (!Helpers.CheckKarmaMessage(Api, message)) return;
                    //Add karma
                    if (message.Text!.StartsWith("+") || message.Text.StartsWith("👍"))
                    {
                        Helpers.SendPlusKarma(Api, message);
                    }
                    //Subtract karma
                    if (message.Text.StartsWith("-") || message.Text.StartsWith("👎"))
                    {
                        Helpers.SendMinusKarma(Api, message);
                    }
                }
                //Check if message starts with like or dislike emoji
                else if (message.Text.StartsWith("👍") || message.Text.StartsWith("👎"))
                {
                    if (!Helpers.CheckKarmaMessage(Api, message)) return;
                    //Add karma
                    if (message.Text!.StartsWith("+") || message.Text.StartsWith("👍"))
                    {
                        Helpers.SendPlusKarma(Api, message);
                    }
                    //Subtract karma
                    if (message.Text.StartsWith("-") || message.Text.StartsWith("👎"))
                    {
                        Helpers.SendMinusKarma(Api, message);
                    }
                }
            }
        }

        base.OnMessage(message);
    }
}