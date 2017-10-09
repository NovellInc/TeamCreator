using System.Collections.Generic;
using DataModels.Extensions;
using DataModels.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DataModels.Models
{
    /// <summary>
    /// Модель хранилища MongoDB.
    /// </summary>
    public class MongoRepository : IRepository
    {
        /// <summary>
        /// Поле идентификатора в хранилище.
        /// </summary>
        private const string IdField = "_id";

        /// <summary>
        /// База данных в MongoDb.
        /// </summary>
        private readonly IMongoDatabase _database;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MongoRepository"/>.
        /// </summary>
        /// <param name="mongoUrl">Строка подключения к MongoDb.</param>
        public MongoRepository(MongoUrl mongoUrl)
        {
            IMongoClient mongoClient = new MongoClient(mongoUrl);
            this._database = mongoClient.GetDatabase(mongoUrl.DatabaseName);
        }

        /// <summary>
        /// Получает список объектов согласно фильтру.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="model">Модель объекта для поиска по указанным данным.</param>
        /// <param name="page">Номер страницы из выборки элементов.</param>
        /// <param name="items">Количество элементов на странице.</param>
        /// <returns>Возвращает список объектов.</returns>
        public List<TModel> Get<TModel>(TModel model, int page = 1, int items = 0)
        {
            if (page < 1)
            {
                return null;
            }

            if (items == 0)
            {
                return this.GetCollection<TModel>().Find(model.ToMongoFilter()).ToList();
            }

            return this.GetCollection<TModel>().Find(model.ToMongoFilter())
                                               .Skip((page - 1) * items)
                                               .Limit(items)
                                               .ToList();
        }

        /// <summary>
        /// Добавляет элемент в хранилище.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="model">Элемент.</param>
        public ObjectId Add<TModel>(TModel model) where TModel : class, IMongoModel
        {
            model.Id = ObjectId.GenerateNewId();
            this.GetCollection<TModel>().InsertOne(model);
            return model.Id;
        }

        /// <summary>
        /// Обновляет элемент в хранилище.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="model">Обновлённый элемент.</param>
        public void Update<TModel>(TModel model) where TModel : class, IMongoModel
        {
            var filter = Builders<TModel>.Filter.Eq(IdField, model.Id);
            this.GetCollection<TModel>().ReplaceOne(filter, model, new UpdateOptions { IsUpsert = true });
        }

        /// <summary>
        /// Удаляет элемент из хранилища.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="id">Идентификатор элемента.</param>
        public void Delete<TModel>(ObjectId id)
        {
            var filter = Builders<TModel>.Filter.Eq(IdField, id);
            this.GetCollection<TModel>().DeleteOne(filter);
        }

        /// <summary>
        /// Получает коллекцию по типу модели.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <returns>Возвращает коллекцию.</returns>
        private IMongoCollection<TModel> GetCollection<TModel>()
        {
            return this._database.GetCollection<TModel>(typeof(TModel).Name);
        }
    }
}
