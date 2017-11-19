using System;
using DataModels.Enums;
using DataModels.Helpers;
using DataModels.Interfaces;
using Extensions;
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
        /// Идентификатор первой команды.
        /// </summary>
        private ObjectId _firstTeamId;

        /// <summary>
        /// Идентификатор второй команды.
        /// </summary>
        private ObjectId _secondTeamId;

        /// <summary>
        /// Идентификатор спортивной площадки.
        /// </summary>
        private ObjectId _sportGroundId;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Game"/>.
        /// </summary>
        public Game()
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
        /// Получает или задает идентификатор создателя игры <see cref="Player"/>.
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
        /// Получает или задает идентификатор первой команды <see cref="Team"/>.
        /// </summary>
        [JsonConverter(typeof(BsonObjectIdConverter))]
        public ObjectId FirstTeamId
        {
            get
            {
                return this._firstTeamId;
            }
            set
            {
                this._firstTeamId = value;
                this.HasFirstTeam = value.IsDefault();
            }
        }

        /// <summary>
        /// Признак наличия первой команды.
        /// </summary>
        public bool HasFirstTeam { get; private set; }

        /// <summary>
        /// Получает или задает идентификатор второй команды <see cref="Team"/>.
        /// </summary>
        [JsonConverter(typeof (BsonObjectIdConverter))]
        public ObjectId SecondTeamId
        {
            get
            {
                return this._secondTeamId;
            }
            set
            {
                this._secondTeamId = value;
                this.HasSecondTeam = value.IsDefault();
            }
        }

        /// <summary>
        /// Признак наличия второй команды.
        /// </summary>
        public bool HasSecondTeam { get; private set; }

        /// <summary>
        /// Получает или задает идентификатор спортивной площадки игры <see cref="SportGround"/>.
        /// </summary>
        [JsonConverter(typeof (BsonObjectIdConverter))]
        public ObjectId SportGroundId
        {
            get
            {
                return this._sportGroundId;
            }
            set
            {
                this._sportGroundId = value;
                this.HasSportGround = value.IsDefault();
            }
        }

        /// <summary>
        /// Признак наличия спортивной площадки игры.
        /// </summary>
        public bool HasSportGround { get; private set; }
    }
}
