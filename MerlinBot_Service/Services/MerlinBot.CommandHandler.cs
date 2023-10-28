using MerlinBot_Service.Controllers;
using MerlinBot_Service.Models;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableMethods.FormattingOptions;
using Telegram.BotAPI.AvailableTypes;
using static MerlinBot_Service.Stuff.Messages;
using static MerlinBot_Service.Stuff.Gifs;

namespace MerlinBot_Service.Services;

public partial class MerlinBotService
{
    protected override void OnCommand(Message message, string commandName, string commandParameters)
    {
        var db = new BotContext();
        var args = commandParameters.Split(' ');
        
#if DEBUG
        _logger.LogInformation("Params: {0}", args.Length);
#endif

        switch (commandName)
        {
            case "setkarma":
            {
                if (args.Length is 3)
                {
                    var messageFrom = db.Users!.FirstOrDefault(c=> c.UserId == message.From!.Id);
                    if (message.Chat.Type==ChatType.Private && messageFrom!.UserType=="admin")
                    {
                        if (args[0].StartsWith("@"))
                        {
                            var karma = Convert.ToInt32(args[1]);
                            var chatName = args[2];
                            var username = args[0].Trim();
                            Helpers.SetKarmaCountAsAdmin( Api, username, karma, chatName); 
                        }
                        else
                        {
                            Api.SendMessage(
                                chatId: message.Chat.Id,
                                text: "You write the command incorrectly...",
                                replyToMessageId: message.MessageId);
                        }
                    }
                    else
                    {
                        Api.SendMessage(
                            chatId: message.Chat.Id,
                            text: "You are not allowed to use this command",
                            replyToMessageId: message.MessageId);
                    }
                }
                else
                {
                    Api.SendMessage(
                        chatId: message.Chat.Id,
                        text: "You write the command incorrectly...",
                        replyToMessageId: message.MessageId);
                }
                
            }
                break;
            
            case "meaning":
            {
                var result = UrbanDictionaryController.SearchForWord(args[0].ToLower()).Result?.list
                    .Take(5);
                switch (result!.Count())
                {
                    case 0:
                        Api.SendMessage(
                            chatId: message.Chat.Id,
                            text:
                            "Вибачте, такого слова не було знайдено. Перевірте правильність написання слова і команди (має бути лише 1 пробіл між командою та словом)\n" +
                            "Правильне застосування команди: /meaning *термін, слово*\n" +
                            "Наприклад, /meaning lmao",
                            replyToMessageId: message.MessageId
                        );
                        break;
                    case 1:
                        var one = UrbanDictionaryController.SearchForWord(args[0].ToLower()).Result?.list
                            .FirstOrDefault();
                        Api.SendMessage(
                            chatId: message.Chat.Id,
                            text: $"Слово: {one.word}\n\n" +
                                  $"Пояснення: {one.definition}\n\n" +
                                  $"Приклад: {one.example}\n\n" +
                                  $"Автор пояснення: {one.author}\n" +
                                  $"Дата: {one.written_on}\n" +
                                  $"Посилання: {one.permalink}",
                            replyToMessageId: message.MessageId
                        );
                        break;

                    case > 1:
                        var buttonItem = result!.Select(c => c.defid.ToString()).ToList();
                        var keyboard =
                            new InlineKeyboardMarkup(
                                UrbanDictionaryController.GetInlineKeyboardForUrbanDictionary(buttonItem));

                        Api.SendMessage(
                            chatId: message.Chat.Id,
                            text: "Є кілька пояснень цього слова!",
                            replyToMessageId: message.MessageId,
                            replyMarkup: keyboard
                        );
                        break;
                }
            }

                break;

            case "karma_get":
                if (message.Chat.Type == ChatType.Private)
                {
                    var text = "Рейтинг карми у всіх чатах 📈 \n\n";

                    var result =
                        from x in db.UsersToChats
                        where x.UserId == message.From.Id
                        orderby x.KarmaCount descending
                        join c in db.Chats on x.ChatId equals c.ChatId into pol
                        from p in pol
                        select new
                        {
                            x.KarmaCount, p.ChatName
                        };
                    var i = 1;

                    foreach (var one in result)
                    {
                        text += $"{i}. <b>{one.ChatName}</b> — <b>{one.KarmaCount}</b>\n";
                        ++i;
                    }

                    Api.SendMessage(
                        chatId: message.Chat.Id,
                        text: text,
                        parseMode: ParseMode.HTML
                    );
                }
                else
                {
                    var user = db.UsersToChats!.FirstOrDefault(c => c.UserId == message.From!.Id
                                                                    && c.ChatId == message.Chat.Id);

                    Api.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"👋 Твоя карма — <b>{user!.KarmaCount}</b>",
                        replyToMessageId: message.MessageId,
                        parseMode: ParseMode.HTML
                    );
                }

                break;

            case "karma_top":
                if (message.Chat.Type == ChatType.Private)
                {
                    Api.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Команда доступна лише в групових чатах \n" +
                              "Щоб дізнатись ваш стан справ по кармі використайте команду /karma_get",
                        replyToMessageId: message.MessageId
                    );
                }
                else
                {
                    var text = "Рейтинг карми 📈 \n\n";

                    var result =
                        from x in db.UsersToChats
                        where x.ChatId == message.Chat.Id
                        orderby x.KarmaCount descending
                        join c in db.Users on x.UserId equals c.UserId into pol
                        from p in pol
                        select new
                        {
                            x.KarmaCount, p.Username, p.FullName
                        };
                    var i = 1;
                    foreach (var one in result)
                    {
                        text += $"{i}. <b>{one.FullName}</b> ({one.Username}) — <b>{one.KarmaCount}</b>\n";
                        ++i;
                    }

                    Api.SendMessage(
                        chatId: message.Chat.Id,
                        text: text,
                        parseMode: ParseMode.HTML
                    );
                }

                break;

            case "start":
                Api.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Привіт, це <b>Merlin</b> бот. Трішки розкажу про те, що я вмію \n" +
                          "Моє головне завдання — рахувати карму, жартувати жарти та пиздити русню)) \n" +
                          "Реагую на фрази \"За цю групу\", \"Слава Україні\" та \"Русні пизда\" \n" +
                          "Також доступні команди:\n\n" +
                          "<i><b>/everyone</b></i> - Скликає всіх учасників чату (за умови, що вони хоча б раз надсилали команду до бота)\n" +
                          "<i><b>/huyak</b></i> - Зменщити популяцію русні на планеті. Просто час від часу використовуйте цю команду...\n" +
                          "<i><b>/joke</b></i> - Випадковий жарт англійською мовою\n" +
                          "<i><b>/help</b></i> - ще раз нагадати про доступні команди :)\n\n" +
                          "Наявні міні-ігри:\n" +
                          "<i><b>/ctrl_game</b></i> - Гра \"Буфер обміну\" \n" +
                          "<i><b>/byblo</b></i> - Гра \"Библо дня\". Правила по команді <i><b>/byblo_rules</b></i>\n\n" +
                          "Бот знаходиться на стадії розробки, тому можливі тимчасові відключення та баги. Очікуйте на новий функціонал в подальшому\n" +
                          "Також можна писати свої ідеї для бота безпосередньо мені - @merlinovaboroda17",
                    replyToMessageId: message.MessageId,
                    parseMode: ParseMode.HTML);
                break;

            case "byblo":
                Helpers.PlayBydloGame(message, Api);
                break;

            case "byblo_top":
                Helpers.TopUsersInBydloGame(message, Api);
                break;

            case "byblo_reg":
                Helpers.SaveUserInBydloGame(message, Api);
                break;

            case "byblo_rules":
                Api.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Правила гри <b>Библо Дня</b> (тільки для групових чатів): \n" +
                          "1. Зареєструйтеся в грі за допомогою команди /byblo_reg 🤌\n" +
                          "2. Зачекайте, поки зареєструються всі (або більшість 🙄)\n" +
                          "3. Запустити гру командою /byblo 🎲\n" +
                          "4. Перегляд статистики в чаті за допомогою команди /byblo_top 📈\n\n" +
                          "<b>Важливо</b>, гру можна проводити лише <b>раз на добу</b>",
                    parseMode: ParseMode.HTML
                );
                break;

            case "everyone":
                if (db.UsersToChats!.Count(c => c.ChatId == message.Chat.Id) < 2)
                {
                    Api.SendMessage(
                        chatId: message.Chat.Id,
                        text:
                        "Менше 2 людей зареєстровано в системі, нехай інші учасники теж спробують використати якусь команду з чату",
                        replyToMessageId: message.ReplyToMessage != null
                            ? message.ReplyToMessage.MessageId
                            : message.MessageId
                    );
                    Console.WriteLine($"There were less than two registered users in the chat: {message.Chat.Id}");
                }
                else
                {
                    var admins = Api.GetChatAdministrators(message.Chat.Id);
                    if (admins.Any(c => c.User.Id == message.From!.Id))
                    {
                        var users = string.Empty;
                        foreach (var utc in db.UsersToChats!.Where(
                                     c => c.ChatId == message.Chat.Id && c.UserId != message.From!.Id))
                        {
                            foreach (var user in db.Users!.Where(c => c.UserId == utc.UserId))
                            {
                                users = users + "@" + user.Username + " ";
                            }
                        }

                        Api.SendMessage(
                            chatId: message.Chat.Id,
                            text: "Кислий борщ мені в стаканчик!😳\n" + users + "\nДо вас тут звертаються!",
                            parseMode: ParseMode.HTML,
                            replyToMessageId: message.ReplyToMessage != null
                                ? message.ReplyToMessage.MessageId
                                : message.MessageId
                        );
                    }
                    else
                    {
                        Api.SendMessage(
                            chatId: message.Chat.Id,
                            text: "Команда лише для адміністраторів чату"
                        );
                    }
                }

                break;

            case "huyak":
                var rand = new Random();
                var randomIndexGif = rand.Next(0, GifsHuyak.OrderBy(_ => Guid.NewGuid()).ToArray().Length);

                var randomIndexCaption = rand.Next(0, RusnyaMessages.OrderBy(_ => Guid.NewGuid()).ToArray().Length);

                Api.SendAnimation(
                    chatId: message.Chat.Id,
                    animation: GifsHuyak[randomIndexGif],
                    caption: RusnyaMessages[randomIndexCaption]
                );

                break;

            case "joke":
                var joke = Helpers.GetJokeAdvanced();

                if (joke.Result!.joke != null)
                    Api.SendMessage(
                        chatId: message.Chat.Id,
                        text: joke.Result.joke
                    );

                break;

            case "ctrl_game":
                Helpers.PlayCtrlGame(message, Api);
                break;

            case "help":
                Api.SendMessage(
                    chatId: message.Chat.Id,
                    text:
                    "Моє головне завдання — рахувати карму, жартувати жарти та пиздити русню)) \n" +
                    "Реагую на фрази \"За цю групу\", \"Слава Україні\" та \"Русні пизда\" \n" +
                    "Також доступні команди:\n\n" +
                    "<i><b>/everyone</b></i> - Скликає всіх учасників чату (за умови, що вони хоча б раз надсилали команду до бота)\n" +
                    "<i><b>/huyak</b></i> - Зменщити популяцію русні на планеті. Просто час від часу використовуйте цю команду...\n" +
                    "<i><b>/joke</b></i> - Випадковий жарт англійською мовою\n" +
                    "<i><b>/help</b></i> - ще раз нагадати про доступні команди :)\n\n" +
                    "Наявні міні-ігри:\n" +
                    "<i><b>/ctrl_game</b></i> - Гра \"Буфер обміну\" \n" +
                    "<i><b>/byblo</b></i> - Гра \"Библо дня\". Правила по команді <i><b>/byblo_rules</b></i>\n\n" +
                    "Бот знаходиться на стадії розробки, тому можливі тимчасові відключення та баги. Очікуйте на новий функціонал в подальшому\n" +
                    "Також можна писати свої ідеї для бота безпосередньо мені - @merlinovaboroda17",
                    replyToMessageId: message.MessageId,
                    parseMode: ParseMode.HTML);
                break;
        }
    }
}