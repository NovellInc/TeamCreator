using MongoDB.Bson;

namespace DataModels.Interfaces
{
    /// <summary>
    /// Интерфейс для базовой модели хранимой в MongoDB.
    /// </summary>
    public interface IMongoModel
    {
        /// <summary>
        /// Получает или задает идентификатор сущности.
        /// </summary>
        ObjectId Id { get; set; }
    }
}
