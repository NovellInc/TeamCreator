namespace DataModels.Models
{
    /// <summary>
    /// Пользователь.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Получает или задает идентификатор пользователя в Telegram.
        /// </summary>
        public int TelegramId { get; set; }

        /// <summary>
        /// Получает или задаетпсевдоним пользователя.
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// Получает или задает имя пользователя.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Получает или задает фамилию пользователя.
        /// </summary>
        public string Surname { get; set; }
    }
}
