using System.Reflection;
using System.Text.RegularExpressions;
using MerlinBot_Service.Models;
using Newtonsoft.Json;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableMethods.FormattingOptions;
using Telegram.BotAPI.AvailableTypes;
using Chat = MerlinBot_Service.Models.Chat;
using User = Telegram.BotAPI.AvailableTypes.User;
using File = System.IO.File;
using UserInDb = MerlinBot_Service.Models.User;
using static MerlinBot_Service.Stuff.KarmaEasterEggs;
using static MerlinBot_Service.Stuff.Messages;

namespace MerlinBot_Service;

public class Helpers
{
    private static readonly string? ProjectName = Assembly.GetEntryAssembly()!.GetName().Name;
    private const Environment.SpecialFolder Folder = Environment.SpecialFolder.LocalApplicationData;
    public static readonly string PathToProjectFiles = Environment.GetFolderPath(Folder) + $"/{ProjectName}/";
    private static readonly string PathToJsonFiles = Path.Join(PathToProjectFiles, "ChatsJson/");

    public static void CheckSettingsFile()
    {
        if (!Directory.Exists(PathToJsonFiles))
        {
            Directory.CreateDirectory(PathToJsonFiles);
#if DEBUG
            Console.WriteLine(
                $"The work directory was created successfully at {Directory.GetCreationTime(PathToJsonFiles)}. " +
                $"Work path: {PathToJsonFiles}");
        }
        else
        {
            Console.WriteLine("Work directory already exists.");
        }
#endif

        var db = new BotContext();
        var chatsNames = db.Chats!.Select(c => c.ChatName).ToList();

        foreach (var chatName in chatsNames)
        {
            var file = PathToJsonFiles + chatName + ".json";
            if (!File.Exists(file))
            {
                var lfw = new LastBybloWinner { Username = "", LastWinnerDate = DateTime.Today.AddDays(-1) };
                File.WriteAllTextAsync(file, JsonConvert.SerializeObject(lfw, Formatting.Indented));
#if DEBUG
                Console.Out.WriteLineAsync(
                    $"File '{chatName}.json' created successfully at {File.GetCreationTime(file)}");
            }
            else
            {
                Console.Out.WriteLineAsync($"File '{chatName}.json' already exists");
            }
#endif
        }
    }

    #region UserFunctional

    public static void SavePrivateChat(User? user)
    {
        using var db = new BotContext();
        var fromId = user!.Id;

        //Return if user already exists in database
        if (db.Users!.Any(c => c.UserId == fromId)) return;

        //If no user in db we have to add him
        var userToDb = new UserInDb
        {
            UserId = fromId,
            FullName = user.FirstName,
            Username = user.Username,
            UserType = "user",
            TimeZone = TimeZoneInfo.Local.Id
        };

        db.Add(userToDb);
        db.SaveChanges();
        Console.WriteLine($"Added user {user.Username} with id '{user.Id}' at the first time");
    }

    public static void SaveChat(Message message)
    {
        CheckSettingsFile();
        //save user from chat
        SavePrivateChat(message.From);

        using var db = new BotContext();
        if (message.From == null) return;

        var fromId = message.From.Id;
        var chatId = message.Chat.Id;

        //Check if chat exists in database
        if (!db.Chats!.Any(c => c.ChatId == chatId))
        {
            //check title for special symbols and replace them with "-"
            var shortTitle = message.Chat.Title;
            if (message.Chat.Title!.Length > 20)
            {
                shortTitle = message.Chat.Title[..20];
            }

            var rgx = new Regex("[*'\",_&#^@]");
            shortTitle = rgx.Replace(shortTitle!, string.Empty);

            var chat = new Chat
            {
                ChatId = chatId,
                ChatName = shortTitle,
                Username = message.Chat.Username ?? "username",
                TimeZone = TimeZoneInfo.Local.Id
            };
            db.Add(chat);
            db.SaveChanges();
            Console.WriteLine($"Added chat {chat.ChatId} at first time");
        }

        //Check if user from chat attached to this chat
        if (db.UsersToChats!.Any(c => c.ChatId == chatId && c.UserId == fromId)) return;
        var utс = new UsersToChats
        {
            UserId = fromId,
            ChatId = chatId,
            KarmaCount = 0,
            UpdatedAt = DateTime.UtcNow
        };
        db.Add(utс);
        db.SaveChanges();

        Console.WriteLine($"Table 'UsersToChats' has been updated for chat: {utс.ChatId}");
    }

    public static void RemoveUserFromDatabases(Message message, ITelegramUser? leftUser)
    {
        using var db = new BotContext();

        var user = db.Users!.FirstOrDefault(c => c.UserId == leftUser!.Id);
        var chat = db.Chats!.FirstOrDefault(c => c.ChatId == message.Chat.Id);

        var utc = db.UsersToChats!.FirstOrDefault(c => c.ChatId == chat!.ChatId && c.UserId == user!.UserId);
        if (utc != null) db.Remove(utc);

        var bybloGame = db.BydloGames!.FirstOrDefault(c => c.ChatId == chat!.ChatId && c.UserId == user!.UserId);
        if (bybloGame != null) db.Remove(bybloGame);

        db.SaveChanges();
    }

    #endregion

    #region BybloGame

    public static void PlayBydloGame(Message message, BotClient bot)
    {
        using var db = new BotContext();
        var chatId = message.Chat.Id;
        var chat = db.Chats!.FirstOrDefault(c => c.ChatId == chatId);
        var file = PathToJsonFiles + chat!.ChatName + ".json";

        if (message.Chat.Type == ChatType.Private)
        {
            bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Команда доступна лише в групових чатах",
                replyToMessageId: message.MessageId
            );
            return;
        }

        var now = DateTime.Today.Date;
        var diff = (now - chat.BydloCommandUsage).TotalDays;

        if (diff < 1)
        {
            var response = File.ReadAllText(file);
            var bybloWinner = JsonConvert.DeserializeObject<LastBybloWinner>(response);
            bot.SendMessage(
                chatId: chatId,
                text: $"Сьогодні библом дня була ця людина - <b>{bybloWinner!.Username}</b>, спробуйте завтра 🤔",
                parseMode: ParseMode.HTML
            );
            return;
        }

        var random = new Random();

        var result =
            from x in db.BydloGames
            where x.ChatId == chatId
            join c in db.Users on x.UserId equals c.UserId into pol
            from p in pol
            select new
            {
                x.Count, x.UserId, p.Username
            };
        var list = result.ToList();

        if (list.Count < 3)
        {
            bot.SendMessage(
                chatId: chatId,
                text:
                "Менше 3 людей зареєстровано в грі, нехай інші учасники теж зареєструються",
                replyToMessageId: message.ReplyToMessage != null
                    ? message.ReplyToMessage.MessageId
                    : message.MessageId
            );
            Console.WriteLine($"There were less than two registered users in the chat: {chatId}");
            return;
        }

        var randomUser = list[random.Next(list.Count)];

        var bGameUser = db.BydloGames!.FirstOrDefault(c => c.UserId == randomUser.UserId && c.ChatId == chatId);
        chat.BydloCommandUsage = now;
        bGameUser!.Count += 1;

        db.SaveChangesAsync();

        var ufg = new LastBybloWinner { Username = "@" + randomUser.Username, LastWinnerDate = now };
        File.WriteAllText(file, JsonConvert.SerializeObject(ufg, Formatting.Indented));

        bot.SendMessageAsync(
            chatId: chatId,
            text: BydloGame1Messages[random.Next(BydloGame1Messages.Length)],
            parseMode: ParseMode.HTML
        );
        Task.Delay(2000).Wait();

        bot.SendMessageAsync(
            chatId: chatId,
            text: BydloGame2Messages[random.Next(BydloGame2Messages.Length)],
            parseMode: ParseMode.HTML
        );
        Task.Delay(2000).Wait();

        bot.SendMessageAsync(
            chatId: chatId,
            text: BydloGame3Messages[random.Next(BydloGame3Messages.Length)] + $"@{randomUser.Username}",
            parseMode: ParseMode.HTML
        );
    }

    public static void TopUsersInBydloGame(Message message, BotClient bot)
    {
        using var db = new BotContext();
        var chatId = message.Chat.Id;

        if (message.Chat.Type == ChatType.Private)
        {
            bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Команда доступна лише в групових чатах \n" +
                      "Щоб дізнатись ваш стан справ по кармі використайте команду /byblo_top",
                replyToMessageId: message.MessageId
            );
        }
        else
        {
            var text = "Рейтинг гравців \"Библо дня\" 📈 \n\n";

            var result =
                from x in db.BydloGames
                where x.ChatId == chatId
                orderby x.Count descending
                join c in db.Users on x.UserId equals c.UserId into pol
                from p in pol
                select new
                {
                    x.Count, x.UserId, p.Username, p.FullName
                };
            var i = 1;
            foreach (var one in result)
            {
                text += $"{i}. <b>{one.FullName}</b> ({one.Username}) був библом — <b>{one.Count}</b> раз(ів)\n";
                ++i;
            }

            bot.SendMessage(
                chatId: message.Chat.Id,
                text: text,
                parseMode: ParseMode.HTML
            );
        }
    }

    public static void SaveUserInBydloGame(Message message, BotClient bot)
    {
        using var db = new BotContext();
        var fromId = message.From!.Id;
        var chatId = message.Chat.Id;

        if (db.BydloGames!.Any(c => c.ChatId == chatId && c.UserId == fromId))
            bot.SendMessage(
                chatId: chatId,
                text: "Ви вже зареєстровані 🤔",
                replyToMessageId: message.MessageId,
                parseMode: ParseMode.HTML
            );
        else
        {
            var bGame = new BydloGame()
            {
                UserId = fromId,
                ChatId = chatId,
                Count = 0
            };
            db.Add(bGame);
            db.SaveChanges();

            bot.SendMessage(
                chatId: chatId,
                text: $"@{message.From.Username} успішно зареєструвався в грі \"Библо дня\" 🤙",
                replyToMessageId: message.MessageId,
                parseMode: ParseMode.HTML
            );

            Console.WriteLine($"Table 'BydloGames' has been updated for chat: {bGame.ChatId}");
        }
    }

    #endregion

    #region KarmaFuntional

    public static bool CheckKarmaMessage(BotClient bot, Message message)
    {
        var charString = message.Text!.ToCharArray();
        
        if (charString.Length > 1)
        {
            if (char.IsLetter(charString[1]) || charString[1] == ' ')
            {
                return false;
            }
        }

        //Check if user replied to Bot
        if (message.ReplyToMessage!.From!.IsBot)
        {
            bot.SendMessage(
                chatId: message.Chat.Id,
                text: "В ботів не може бути карми!\n" +
                      "То не люди, а комплюктори..."
            );
            return false;
        }

        //Check if user replied to himself
        if (message.From!.Id == message.ReplyToMessage!.From!.Id)
        {
            bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Ага, ще чого...",
                replyToMessageId: message.MessageId
            );
            return false;
        }

        return true;
    }

    public static void SendPlusKarma(BotClient bot, Message message)
    {
        var db = new BotContext();
        var karmaTo = db.UsersToChats!.FirstOrDefault(c =>
            c.UserId == message.ReplyToMessage!.From!.Id && c.ChatId == message.Chat.Id);
        var karmaFrom =
            db.UsersToChats!.FirstOrDefault(c => c.UserId == message.From!.Id && c.ChatId == message.Chat.Id);
        var random = new Random();

        if (karmaTo == null || karmaFrom == null) return;

        var now = DateTime.UtcNow;
        var diff = now.Subtract(karmaFrom.UpdatedAt);

        if (diff.TotalSeconds < 15)
        {
            bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Пройшло надто мало часу від останньої спроби змінити комусь карму 🤧",
                replyToMessageId: message.MessageId,
                parseMode: ParseMode.HTML
            );
            return;
        }

        karmaTo.KarmaCount += 1;
        karmaFrom.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();

        bot.SendMessage(
            chatId: message.Chat.Id,
            text: $"<b>{message.From!.FirstName} {message.From.LastName}</b> респектує " +
                  $"<b>{message.ReplyToMessage!.From!.FirstName} {message.ReplyToMessage.From.LastName}</b> " +
                  $"за цей меседж та додає карми 💚\n" +
                  $"Тепер її: <b>{karmaTo.KarmaCount}</b>",
            parseMode: ParseMode.HTML
        );


        switch (karmaTo.KarmaCount.ToString())
        {
            case "69":
                bot.SendPhoto(
                    chatId: message.Chat.Id,
                    photo: Karma69Photos[random.Next(Karma69Photos.Length)],
                    caption: "Шлях до істини лежить через..."
                );

                break;

            case "420":
                bot.SendPhoto(
                    chatId: message.Chat.Id,
                    photo: Karma420Photos[random.Next(Karma420Photos.Length)],
                    caption: "Не дуже секретна знахідка",
                    replyToMessageId: message.ReplyToMessage.MessageId
                );

                break;

            case "665":
                bot.SendPhoto(
                    chatId: message.Chat.Id,
                    photo: "https://imgur.com/Y3xhaUq"
                );

                break;

            case "666":
                bot.SendPhoto(
                    chatId: message.Chat.Id,
                    photo: Karma666Photos[random.Next(Karma666Photos.Length)],
                    caption: "І який сенс було збільшувати тобі карму аж на стільки?",
                    replyToMessageId: message.ReplyToMessage.MessageId
                );

                break;
        }
    }

    public static void SendMinusKarma(BotClient bot, Message message)
    {
        var db = new BotContext();
        var karmaTo = db.UsersToChats!.FirstOrDefault(c =>
            c.UserId == message.ReplyToMessage!.From!.Id && c.ChatId == message.Chat.Id);
        var karmaFrom =
            db.UsersToChats!.FirstOrDefault(c => c.UserId == message.From!.Id && c.ChatId == message.Chat.Id);

        if (karmaTo == null || karmaFrom == null) return;

        var now = DateTime.UtcNow;
        var diff = now.Subtract(karmaFrom.UpdatedAt);

        if (diff.TotalSeconds < 15)
        {
            bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Пройшло надто мало часу від останньої спроби змінити комусь карму 🤧",
                replyToMessageId: message.MessageId
            );
            return;
        }

        karmaTo.KarmaCount -= 1;
        karmaFrom.UpdatedAt = now;
        db.SaveChanges();

        bot.SendMessage(
            chatId: message.Chat.Id,
            text: $"<b>{message.From?.FirstName} {message.From?.LastName}</b> не вподобав повідомлення " +
                  $"<b>{message.ReplyToMessage?.From?.FirstName} {message.ReplyToMessage?.From?.LastName}</b>. " +
                  $"Знімемо трохи карми 🙄\n" +
                  $"Тепер її: <b>{karmaTo.KarmaCount}</b>",
            parseMode: ParseMode.HTML
        );
    }

    #endregion

    public static void PlayCtrlGame(Message message, BotClient bot)
    {
        var random = new Random();
        var db = new BotContext();

        var chatId = message.Chat.Id;
        var from = message.From;

        if (db.UsersToChats!.Count(c => c.ChatId == chatId) < 2)
        {
            bot.SendMessage(
                chatId: chatId,
                text:
                "Менше 2 людей зареєстровано в системі, нехай інші учасники теж спробують використати якусь команду з чату",
                replyToMessageId: message.ReplyToMessage != null
                    ? message.ReplyToMessage.MessageId
                    : message.MessageId
            );
            Console.WriteLine($"There were less than two registered users in the chat: {chatId}");
            return;
        }

        var usernames = new List<string>();
        foreach (var utc in db.UsersToChats!.Where(c => c.ChatId == chatId))
        {
            foreach (var user in db.Users!.Where(c => c.UserId == utc.UserId && c.UserId != from!.Id))
            {
                usernames.Add(user.Username);
            }
        }

        var randomUsernamesList = usernames.OrderBy(_ => Guid.NewGuid()).Take(5).ToList();
        randomUsernamesList.Add(from!.Username!);

        var names = (from username in randomUsernamesList
            let firstName = db.Users!.FirstOrDefault(c => c.Username == username)!.FullName
            select $"{firstName} (@{username})").ToList();

        bot.SendPhoto(
            chatId: chatId,
            photo: CtrlPhotos[random.Next(CtrlPhotos.Length)],
            caption: "Офісна гра \"буфер-покер\".\nКожен учасник змушений не думаючи надіслати в чат те, " +
                     "що збрежено в буфері обміну!\nВиграє той, хто надіслав найбільш їбануту, смішну, приколдесну, " +
                     "прикольну, ржачну картинку чи текст чи що тви там зберігаєте\n\n" +
                     $"@{randomUsernamesList.Aggregate((a, b) => a + ", @" + b)}\n\n" +
                     "Голосування створиться за кілька секунд і буде відкрите стільки часу, скільки учасників у грі - 6 хвилин.\n" +
                     "Будь ласка, притримуйтесь правил чесної гри і голосуйте чесно))"
        );

        //Wait 15 seconds and create Poll that will be available 6 minutes
        Task.Delay(15000).Wait();
        bot.SendPoll(
            chatId: chatId,
            question: "У кого найбільш смішна фігня в буфері обміну?",
            options: names,
            protectContent: true,
            openPeriod: 360
        );
    }

    //Joke api usage
    public static async Task<JokeApi.Joke?> GetJokeAdvanced()
    {
        const string baseUrl = "https://v2.jokeapi.dev";
        string[] parameters = { "blacklistFlags=explicit", "type=single" };
        var requestUrl = $"{baseUrl}/joke/Any?{string.Join("&", parameters)}";

        using var httpClient = new HttpClient();
        var json = await httpClient.GetStringAsync(requestUrl);
        var randomJoke = JsonConvert.DeserializeObject<JokeApi.Joke>(json);

        return randomJoke;
    }
}