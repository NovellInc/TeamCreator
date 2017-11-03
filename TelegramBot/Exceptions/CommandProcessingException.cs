using System;

namespace TelegramBot.Exceptions
{
    /// <summary>
    /// Исключение выбрасывается при возникновении ошибки в обработке команды.
    /// </summary>
    public class CommandProcessingException : Exception
    {
        public const string BadData = "Некорректные данные";
        public const string BadGameId = "Некорректный идентификатор игры";
        public const string GameNotExist = "Игра не существует";
        public const string NotCreatorTryDelete = "Только создатель игры может удалить игру";

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CommandProcessingException"/>.
        /// </summary>
        public CommandProcessingException()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CommandProcessingException"/>.
        /// </summary>
        /// <param name="message">Сообщение с описанием ошибки.</param>
        public CommandProcessingException(string message) : base(message)
        {
        }
    }
}
