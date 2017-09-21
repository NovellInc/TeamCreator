using DataModels.Helpers;
using DataModels.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace DataModels.Models
{
    /// <summary>
    /// Класс описывает игрока.
    /// </summary>
    public sealed class Player : User, IMongoModel
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Player"/>.
        /// </summary>
        /// <param name="nickname">Псевдоним игрока.</param>
        public Player(string nickname)
        {
            this.Nickname = nickname;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Player"/>.
        /// </summary>
        /// <param name="telegramId">Идентификатор игрока в Telegram.</param>
        public Player(int telegramId)
        {
            this.TelegramId = telegramId;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Player"/>.
        /// </summary>
        internal Player()
        {
        }

        /// <summary>
        /// Идентификатор в MongoDB.
        /// </summary>
        [BsonId]
        [JsonConverter(typeof(BsonObjectIdConverter))]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Получает или задает город игрока.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Получает или задает навыки игрока.
        /// </summary>
        public Skill[] Skills { get; set; }
    }
}
