using System;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataModels.Helpers
{
    /// <summary>
    /// Конвертирует <see cref="BsonObjectId" /> из JSON в BSON и обратно.
    /// </summary>
    public class BsonObjectIdConverter : JsonConverter
    {
        /// <summary>Записывает JSON представление объекта.</summary>
        /// <param name="writer"><see cref="T:Newtonsoft.Json.JsonWriter" /> </param>
        /// <param name="value">Конвертируемое значением.</param>
        /// <param name="serializer">Используемый серриализатор.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToString());
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            return ObjectId.Parse(token.ToObject<string>());
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof (ObjectId).IsAssignableFrom(objectType);
        }
    }
}