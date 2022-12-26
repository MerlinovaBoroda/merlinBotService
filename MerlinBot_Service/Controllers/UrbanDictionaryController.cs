using MerlinBot_Service.Models;
using Newtonsoft.Json;
using Telegram.BotAPI.AvailableTypes;

namespace MerlinBot_Service.Controllers;

public static class UrbanDictionaryController
{
    public static async Task<UrbanDictionaryModel.Root?> SearchForWord(string word)
    {
        const string baseUrl = "https://api.urbandictionary.com/v0/define?term=";
        var requestUrl = $"{baseUrl}{word}";
        
        using var httpClient = new HttpClient();
        var json = await httpClient.GetStringAsync(requestUrl);
        var result = JsonConvert.DeserializeObject<UrbanDictionaryModel.Root>(json);
        
        return result;
    }
    
    public static async Task<UrbanDictionaryModel.Root?> SearchForWordById(string id)
    {
        const string baseUrl = "https://api.urbandictionary.com/v0/define?defid=";
        var requestUrl = $"{baseUrl}{id}";
        
        using var httpClient = new HttpClient();
        var json = await httpClient.GetStringAsync(requestUrl);
        var result = JsonConvert.DeserializeObject<UrbanDictionaryModel.Root>(json);
        
        return result;
    }
    
    public static InlineKeyboardButton[][] GetInlineKeyboardForUrbanDictionary(IReadOnlyList<string> stringArray)
    {
        var keyboardInline = new InlineKeyboardButton[1][];
        var keyboardButtons = new InlineKeyboardButton[stringArray.Count];
        for (var i = 0; i < stringArray.Count; i++)
        {
            keyboardButtons[i] = new InlineKeyboardButton
            {
                Text = (i+1).ToString(),
                CallbackData = stringArray[i],
            };
        }
        keyboardInline[0] = keyboardButtons;
        return keyboardInline;
    }
}