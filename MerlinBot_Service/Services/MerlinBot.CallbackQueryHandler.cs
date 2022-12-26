using MerlinBot_Service.Controllers;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;

namespace MerlinBot_Service.Services;

public partial class MerlinBotService
{
    protected override void OnCallbackQuery(CallbackQuery callbackQuery)
    {
        var result = UrbanDictionaryController.SearchForWordById(callbackQuery.Data).Result?.list.FirstOrDefault();
        if (result == null) return;

        if (callbackQuery.Message.ReplyToMessage!.From!.Id != callbackQuery.From.Id)
        {
            Api.SendMessage(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"@{callbackQuery.From.Username}, лише той, хто запросив значення слова, може вибирати"
            );
            return;
        }
        
        Api.SendMessage(
            chatId: callbackQuery.Message.Chat.Id,
            text: $"Слово: {result.word}\n\n" +
                  $"Пояснення: {result.definition}\n\n" +
                  $"Приклад: {result.example}\n\n" +
                  $"Автор пояснення: {result.author}\n" +
                  $"Дата: {result.written_on}\n" +
                  $"Посилання: {result.permalink}",
            replyToMessageId: callbackQuery.Message.ReplyToMessage!.MessageId
        );
    }
}