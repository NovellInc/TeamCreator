using System.Collections.Generic;
using MongoDB.Bson;

namespace DataModels.Interfaces
{
    /// <summary>
    /// Интерфейс хранилища.
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// Получает данные согласно фильтру.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="model">Фильтр.</param>
        /// <param name="page">Номер страницы из выборки элементов.</param>
        /// <param name="items">Количество элементов на странице.</param>
        /// <returns></returns>
        List<TModel> Get<TModel>(TModel model, int page = 0, int items = int.MaxValue);

        /// <summary>
        /// Добавляет элемент в хранилище.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="model">Элемент.</param>
        ObjectId Add<TModel>(TModel model) where TModel : class, IMongoModel;

        /// <summary>
        /// Обновляет элемент в хранилище.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="model">Обновлённый элемент.</param>
        void Update<TModel>(TModel model) where TModel : class, IMongoModel;

        /// <summary>
        /// Удаляет элемент из хранилища.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="id">Идентификатор элемента.</param>
        void Delete<TModel>(ObjectId id);
    }
}
