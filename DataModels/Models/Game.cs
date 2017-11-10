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
        internal Game()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Game"/>.
        /// </summary>
        /// <param name="creatorId">Создатель игры.</param>
        public Game(ObjectId creatorId)
        {
            CreatorId = creatorId;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Game"/>.
        /// </summary>
        /// <param name="id">Идентификатор Игры.</param>
        /// <param name="name">Название Игры.</param>
        /// <param name="startTime">Время начала Игры.</param>
        /// <param name="playersPerTeam">Количество игроков в командах.</param>
        public Game(ObjectId id, string name, DateTime startTime, int playersPerTeam)
        {
            Id = id;
            Name = name;
            StartTime = startTime;
            PlayersPerTeam = playersPerTeam;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Game"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="kindOfSport"></param>
        public Game(ObjectId id, KindOfSport kindOfSport)
        {
            Id = id;
            KindOfSport = kindOfSport;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Game"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isPublic"></param>
        public Game(ObjectId id, bool isPublic)
        {
            Id = id;
            IsPublic = isPublic;
        }

        /// <summary>
        /// Идентификатор в MongoDB.
        /// </summary>
        [BsonId]
        [JsonConverter(typeof(BsonObjectIdConverter))]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Получает или задает идентификатор создателя игры.
        /// </summary>
        [JsonConverter(typeof(BsonObjectIdConverter))]
        public ObjectId CreatorId { get; set; }

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
        /// Идентификатор чата (для частных игр).
        /// </summary>
        public long ChatId { get; set; }

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

        /// <summary>
        /// Спортивная площадка игры.
        /// </summary>
        public SportGround SportGround { get; set; }
    }
}
