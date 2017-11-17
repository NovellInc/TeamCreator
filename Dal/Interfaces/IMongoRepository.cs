using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DataModels.Interfaces;
using MongoDB.Bson;

namespace Dal.Interfaces
{
    /// <summary>
    /// Интерфейс хранилища.
    /// </summary>
    public interface IMongoRepository
    {
        /// <summary>
        /// Получает объект по идентификатору.
        /// </summary>
        /// <typeparam name="TModel">Тип объекта.</typeparam>
        /// <param name="id">Идентификатор.</param>
        /// <returns></returns>
        TModel Get<TModel>(ObjectId id) where TModel : IMongoModel;

        /// <summary>
        /// Получает данные согласно фильтру.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="model">Фильтр.</param>
        /// <param name="page">Номер страницы из выборки элементов.</param>
        /// <param name="items">Количество элементов на странице.</param>
        /// <returns></returns>
        List<TModel> Get<TModel>(TModel model, int page = 1, int items = 0);

        /// <summary>
        /// Получает данные согласно фильтру.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="filter">Фильтр, представленный лямбда выражением.</param>
        /// <param name="page">Номер страницы из выборки элементов.</param>
        /// <param name="items">Количество элементов на странице.</param>
        /// <returns></returns>
        List<TModel> Get<TModel>(Expression<Func<TModel, bool>> filter, int page = 1, int items = 0);

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
        /// <param name="model">Обновляющий элемент.</param>
        void Update<TModel>(TModel model) where TModel : class, IMongoModel;

        /// <summary>
        /// Заменяет элемент в хранилище.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="model">Заменяющий элемент.</param>
        void Replace<TModel>(TModel model) where TModel : class, IMongoModel;

        /// <summary>
        /// Удаляет элемент из хранилища.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="id">Идентификатор элемента.</param>
        void Delete<TModel>(ObjectId id);
    }
}
