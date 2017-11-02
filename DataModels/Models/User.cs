namespace DataModels.Models
{
    /// <summary>
    /// Пользователь.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="User"/>.
        /// </summary>
        internal User()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="User"/>.
        /// </summary>
        /// <param name="telegramId">Идентификатор пользователя в Telegram.</param>
        public User(int telegramId)
        {
            TelegramId = telegramId;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="User"/>.
        /// </summary>
        /// <param name="nickname">Псевдоним пользователя в Telegram.</param>
        public User(string nickname)
        {
            Nickname = nickname;
        }

        /// <summary>
        /// Получает или задает идентификатор пользователя в Telegram.
        /// </summary>
        public int TelegramId { get; set; }

        /// <summary>
        /// Получает или задает псевдоним пользователя.
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

        protected bool Equals(User other)
        {
            return TelegramId == other.TelegramId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((User) obj);
        }

        public override int GetHashCode()
        {
            return TelegramId;
        }
    }
}
