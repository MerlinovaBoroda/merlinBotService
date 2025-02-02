﻿using Telegram.BotAPI;
using Telegram.BotAPI.GettingUpdates;

namespace MerlinBot_Service.Services
{
    /// <summary>
    /// It contains the main functionality of the telegram bot. <br />
    /// The application creates a new instance of this class to process each update received.
    /// </summary>
    public partial class MerlinBotService
    {
        public override void OnUpdate(Update update)
        {
#if DEBUG
            _logger.LogInformation("New update with id: {0}. Type: {1}", update.UpdateId, update.Type.ToString("F"));
#endif
            
            base.OnUpdate(update);
        }
    }
}