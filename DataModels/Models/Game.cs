using System;
using DataModels.Enums;
using DataModels.Helpers;
using DataModels.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace DataModels.Models
{
    /// <summary>
    /// Класс представляет игру.
    /// </summary>
    public sealed class Game : IMongoModel
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Game"/>.
        /// </summary>
        /// <param name="creator">Создатель игры.</param>
        public Game(Player creator)
        {
            Creator = creator;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Game"/>.
        /// </summary>
        /// <param name="id">Идентификатор в MongoDB.</param>
        public Game(ObjectId id)
        {
            this.Id = id;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Game"/>.
        /// </summary>
        internal Game()
        {
        }

        /// <summary>
        /// Идентификатор в MongoDB.
        /// </summary>
        [BsonId]
        [JsonConverter(typeof(BsonObjectIdConverter))]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Получает или задает создателя игры.
        /// </summary>
        public Player Creator { get; set; }

        /// <summary>
        /// Получает или задает вид спорта игры.
        /// </summary>
        public KindOfSport KindOfSport { get; set; }

        /// <summary>
        /// Получает или задает название игры.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Получает или задает признак общедоступности игры.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Получает или задает начало игры.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Получает или задает максимальное количество игроков в команде.
        /// </summary>
        public int PlayersPerTeam { get; set; }

        /// <summary>
        /// Получает или задает первую команду.
        /// </summary>
        public Team FirstTeam { get; set; }

        /// <summary>
        /// Получает или задает вторую команду.
        /// </summary>
        public Team SecondTeam { get; set; }
    }
}
