namespace DataModels.Models
{
    /// <summary>
    /// Пользователь.
    /// </summary>
    public class TelegramUser
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TelegramUser"/>.
        /// </summary>
        public TelegramUser()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TelegramUser"/>.
        /// </summary>
        /// <param name="telegramId">Идентификатор пользователя в Telegram.</param>
        public TelegramUser(int telegramId)
        {
            TelegramId = telegramId;
        }
        
        /// <summary>
        /// Получает или задает идентификатор пользователя в Telegram.
        /// </summary>
        public int TelegramId { get; set; }

        /// <summary>
        /// Получает или задает псевдоним игрока в Telegram.
        /// </summary>
        public string TelegramNickname { get; set; }

        /// <summary>
        /// Получает или задает имя игрока в Telegram.
        /// </summary>
        public string TelegramName { get; set; }

        /// <summary>
        /// Получает или задает фамилию игрока в Telegram.
        /// </summary>
        public string TelegramSurname { get; set; }

        /// <summary>
        /// Получает или задает смещение от UTC часового пояса игрока в Telegram.
        /// </summary>
        public int TelegramTimeZone { get; set; }

        /// <summary>
        /// Получает или задает код языка в Telegram.
        /// </summary>
        public string TelegramLanguageCode { get; set; }
        
        protected bool Equals(TelegramUser other)
        {
            return TelegramId == other.TelegramId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TelegramUser) obj);
        }

        public override int GetHashCode()
        {
            return TelegramId;
        }
    }
}
