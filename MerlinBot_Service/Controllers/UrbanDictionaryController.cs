using MerlinBot_Service.Models;
using Newtonsoft.Json;

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
}