using System;
using System.Collections.Generic;
using DataModels.Models;

namespace TelegramBot.Models
{
    /// <summary>
    /// Класс описывает конфигурацию чата.
    /// </summary>
    public class ChatConfiguraion
    {
        /// <summary>
        /// Пользователи чата.
        /// </summary>
        private HashSet<User> _users;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ChatConfiguraion"/>.
        /// </summary>
        /// <param name="chatId">Идентификатор чата.</param>
        /// <param name="timezoneOffset">Часовой пояс.</param>
        /// <param name="creator">Создатель чата.</param>
        public ChatConfiguraion(long chatId, int timezoneOffset, User creator)
        {
            this.ChatId = chatId;
            this.TimezoneOffset = timezoneOffset;
            this.Creator = creator;
            this.Users.Add(this.Creator);
        }

        /// <summary>
        /// Получает идентификатор чата.
        /// </summary>
        public long ChatId { get; }

        /// <summary>
        /// Получает часовой пояс.
        /// </summary>
        public int TimezoneOffset { get; }

        /// <summary>
        /// Получает создателя чата.
        /// </summary>
        public User Creator { get; }

        /// <summary>
        /// Получает или задает последнюю активность чата.
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Получает или задает пользователей чата.
        /// </summary>
        public HashSet<User> Users
        {
            get { return this._users ?? new HashSet<User>(); }
            set { this._users = value; }
        }
    }
}
