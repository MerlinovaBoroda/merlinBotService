using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using static MerlinBot_Service.Stuff.Messages;

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

#if DEBUG
        _logger.LogInformation("New message from chat id: {ChatId}", message.Chat.Id);
        _logger.LogInformation("Message: {MessageContent}",
            !string.IsNullOrEmpty(message.Text) ? message.Text : "No text");
#endif

        // Only private Chats
        if (message.Chat.Type == ChatType.Private)
        {
            Helpers.SavePrivateChat(message.From);
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
            if (message.ReplyToMessage is not null)
            {
                //Check if message is sticker
                if (message.Sticker is not null)
                {
                    //Check if Sticker emoji Contains like or dislike
                    if (message.Sticker.Emoji.Contains("👍") || message.Sticker.Emoji.Contains("👎"))
                    {
                        if (!Helpers.CheckKarmaMessage(Api, message)) return;
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
                }

                //Check if message is text and not empty
                if (message.Text is not null)
                {
                    //Check if message starts with + or -
                    if (message.Text.StartsWith("+") || message.Text.StartsWith("-"))
                    {
                        if (!Helpers.CheckKarmaMessage(Api, message)) return;
                        //Add karma
                        if (message.Text.StartsWith("+"))
                        {
                            Helpers.SendPlusKarma(Api, message);
                        }

                        //Subtract karma
                        if (message.Text.StartsWith("-"))
                        {
                            Helpers.SendMinusKarma(Api, message);
                        }
                    }

                    //Check if message starts with like or dislike emoji
                    else if (message.Text.StartsWith("👍") || message.Text.StartsWith("👎"))
                    {
                        if (!Helpers.CheckKarmaMessage(Api, message)) return;
                        //Add karma
                        if (message.Text.StartsWith("👍"))
                        {
                            Helpers.SendPlusKarma(Api, message);
                        }

                        //Subtract karma
                        if (message.Text.StartsWith("👎"))
                        {
                            Helpers.SendMinusKarma(Api, message);
                        }
                    }
                }
            }
        }

        try
        {
            base.OnMessage(message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}