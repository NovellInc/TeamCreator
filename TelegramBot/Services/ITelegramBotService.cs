using Telegram.Bot;

namespace TelegramBot.Services
{
    /// <summary>
    /// Интерфейс службы телеграм бота.
    /// </summary>
    public interface ITelegramBotService
    {
        /// <summary>
        /// Получает клиента телеграм бота.
        /// </summary>
        TelegramBotClient BotClient { get; }
    }
}
