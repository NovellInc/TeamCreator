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
    public sealed class Player : TelegramUser, IMongoModel
    {
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
        /// <param name="id">Идентификатор в MongoDB.</param>
        public Player(ObjectId id)
        {
            this.Id = id;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Player"/>.
        /// </summary>
        public Player()
        {
        }

        /// <summary>
        /// Идентификатор в MongoDB.
        /// </summary>
        [BsonId]
        [JsonConverter(typeof(BsonObjectIdConverter))]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Получает или задает псевдоним игрока.
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// Получает или задает имя игрока.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Получает или задает фамилию игрока.
        /// </summary>
        public string Surname { get; set; }
        
        /// <summary>
        /// Получает или задает город игрока.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Получает или задает навыки игрока.
        /// </summary>
        public Skill[] Skills { get; set; }

        private bool Equals(Player other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Возвращает отображаемое имя игрока.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return !string.IsNullOrEmpty(this.Nickname)
                ? this.Nickname
                : $"{this.Name} {this.Surname}".Trim();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Player && Equals((Player) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
