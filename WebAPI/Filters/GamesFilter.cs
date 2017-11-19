using System;
using DataModels.Enums;
using DataModels.Helpers;
using DataModels.Models;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace WebAPI.Filters
{
    /// <summary>
    /// Фильтр Игр.
    /// </summary>
    [JsonObject]
    public sealed class GamesFilter
    {
        /// <summary>
        /// Идентификатор в MongoDB.
        /// </summary>
        [JsonProperty(nameof(Game.Id))]
        [JsonConverter(typeof(BsonObjectIdConverter))]
        public ObjectId? Id { get; set; }

        /// <summary>
        /// Получает или задает идентификатор создателя игры.
        /// </summary>
        [JsonProperty(nameof(Game.CreatorId))]
        [JsonConverter(typeof(BsonObjectIdConverter))]
        public ObjectId? CreatorId { get; set; }

        /// <summary>
        /// Получает или задает вид спорта игры.
        /// </summary>
        [JsonProperty(nameof(Game.KindOfSport))]
        public KindOfSport? KindOfSport { get; set; }

        /// <summary>
        /// Получает или задает признак общедоступности игры.
        /// </summary>
        [JsonProperty(nameof(Game.IsPublic))]
        public bool? IsPublic { get; set; }

        /// <summary>
        /// Идентификатор чата (для частных игр).
        /// </summary>
        [JsonProperty(nameof(Game.ChatId))]
        public long? ChatId { get; set; }

        /// <summary>
        /// Получает или задает начало игры.
        /// </summary>
        [JsonProperty(nameof(Game.StartTime))]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Получает или задает максимальное количество игроков в команде.
        /// </summary>
        [JsonProperty(nameof(Game.PlayersPerTeam))]
        public int? PlayersPerTeam { get; set; }

        //public static Game ConvertToGame(GamesFilter filter)
        //{
        //    Game game = new Game();
        //    if (filter.Id.HasValue)
        //    {
                
        //    }
        //}
    }
}
