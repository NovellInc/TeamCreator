using System;
using DataModels.Enums;
using DataModels.Models;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace TelegramBot.Models
{
    /// <summary>
    /// Класс описывает параметры игры.
    /// </summary>
    [JsonObject]
    public sealed class GameParams
    {
        /// <summary>
        /// Идентификатор в MongoDB.
        /// </summary>
        public ObjectId Id { get; set; }

        /// <summary>
        /// Получает или задает участника игры.
        /// </summary>
        public Player Player { get; set; }

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
    }
}
