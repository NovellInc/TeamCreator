using System;

namespace TelegramBot.Exceptions
{
    /// <summary>
    /// Исключение выбрасывается при возникновении ошибки в обработке команды.
    /// </summary>
    public sealed class CommandProcessingException : Exception
    {
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

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CommandProcessingException"/>.
        /// </summary>
        /// <param name="message">Сообщение с описанием ошибки.</param>
        /// <param name="innerException">Внешняя ошибка.</param>
        public CommandProcessingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
