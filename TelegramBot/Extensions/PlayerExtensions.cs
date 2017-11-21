using DataModels.Models;

namespace TelegramBot.Extensions
{
    /// <summary>
    /// Класс расширений для игрока <see cref="Player"/>.
    /// </summary>
    public static class PlayerExtensions
    {
        /// <summary>
        /// Получает ссылку на пользователя Telegram или ФИ.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <returns></returns>
        public static string GetTelegramLink(this Player player)
        {
            return !string.IsNullOrEmpty(player.TelegramNickname)
                ? $"@{player.TelegramNickname}"
                : $"{player.TelegramName} {player.TelegramSurname}".Trim();
        }

        /// <summary>
        /// Получает общее имя игрока.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <returns></returns>
        public static string GetCommonName(this Player player)
        {
            string telegramLink = player.GetTelegramLink();
            return !string.IsNullOrEmpty(telegramLink)
                ? telegramLink
                : player.ToString();
        }
    }
}
