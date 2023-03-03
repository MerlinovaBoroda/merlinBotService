using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace MerlinBot_Service.Models;

public class BotContext : DbContext
{
    public DbSet<User>? Users { get; set; }
    public DbSet<Chat>? Chats { get; set; }
    public DbSet<UsersToChats>? UsersToChats { get; set; }
    public DbSet<BydloGame>? BydloGames { get; set; }

    public string DbPath { get; }

    public BotContext()
    {
        // const Environment.SpecialFolder folder = Environment.SpecialFolder.LocalApplicationData;
        // var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(Helpers.PathToProjectFiles, "MerlinBot.db");
        Console.WriteLine(DbPath);
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        =>  options.UseSqlite($"Data Source={DbPath}");
}

public class User
{
    [Key] public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UserType { get; set; } = "user";
    public string TimeZone { get; set; } = string.Empty;
}

public class Chat
{
    [Key] public long ChatId { get; set; }
    public string ChatName { get; set; } = null!;
    public string Username { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public DateTime BydloCommandUsage { get; set; } = DateTime.Today.AddDays(-1);
}

public class UsersToChats
{
    // ReSharper disable once InconsistentNaming
    public int id { get; set; }
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public int KarmaCount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class BydloGame
{
    [Key] public int Id { get; set; }
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public int Count { get; set; }
}

public class LastBybloWinner
{
    public string Username { get; set; }
    public DateTime LastWinnerDate { get; set; }
}