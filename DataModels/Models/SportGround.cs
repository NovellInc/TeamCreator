using DataModels.Enums;
using DataModels.Helpers;
using DataModels.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace DataModels.Models
{
    /// <summary>
    /// Класс описывает сопртивную площадку.
    /// </summary>
    public sealed class SportGround : IMongoModel
    {
        /// <summary>
        /// Идентификатор в MongoDB.
        /// </summary>
        [BsonId]
        [JsonConverter(typeof(BsonObjectIdConverter))]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Получает или задает название.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Получает или задает адрес.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Получает или задает номер телефона.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Получает или задает электронную почту.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Получает или задает веб-сайт.
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// Получает или задает арендную плату за час.
        /// </summary>
        public double RentPerHour { get; set; }

        /// <summary>
        /// Получает или задает валюту оплаты.
        /// </summary>
        public Currency Currency { get; set; }
    }
}
