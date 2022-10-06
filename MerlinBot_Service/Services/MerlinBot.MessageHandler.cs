using MerlinBot_Service.Controllers;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;

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

        var hasText = !string.IsNullOrEmpty(message.Text); // True if message has text;

#if DEBUG
        _logger.LogInformation("New message from chat id: {ChatId}", message.Chat.Id);
        _logger.LogInformation("Message: {MessageContent}", hasText ? message.Text : "No text");
#endif

        base.OnMessage(message);
        
        var splitedText = message.Text?.Split(' ');
        foreach (var word in splitedText!)
        {
            var list = new List<char>();
            for (var c = 'a'; c <= 'z'; ++c) {
                list.Add(c);
            }

            var letterInWord = word.ToLower().ToCharArray().ToList().FirstOrDefault();
            if (list.Contains(letterInWord))
            {
                var result = await UrbanDictionaryController.SearchForWord(word);
                if (result != null)
                {
                    
                }
            }
            
            Console.WriteLine(word);
        }
    }
}