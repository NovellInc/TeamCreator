using System.Collections.Generic;
using DataModels.Helpers;
using DataModels.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace DataModels.Models
{
    /// <summary>
    /// Класс описывает команду.
    /// </summary>
    public sealed class Team : IMongoModel
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Team"/>.
        /// </summary>
        /// <param name="name">Название команды.</param>
        public Team(string name)
        {
            this.Name = name;
            this.Players = new HashSet<Player>();
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Team"/>.
        /// </summary>
        /// <param name="id">Идентификатор в MongoDB.</param>
        public Team(ObjectId id)
        {
            this.Id = id;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Team"/>.
        /// </summary>
        internal Team()
        {
        }

        /// <summary>
        /// Идентификатор в MongoDB.
        /// </summary>
        [BsonId]
        [JsonConverter(typeof(BsonObjectIdConverter))]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Получает или задает название команды.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Получает или задает игроков команды.
        /// </summary>
        public HashSet<Player> Players { get; set; }

        /// <summary>
        /// Капитан команды.
        /// </summary>
        public Player Captain { get; set; }
    }
}
