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
        internal TelegramUser()
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
