using System;
using System.Linq;
using DataModels.Interfaces;
using MongoDB.Driver;

namespace Dal.Extensions
{
    /// <summary>
    /// Класс содержит обобщенные методы расширения. 
    /// </summary>
    public static class TExtensions
    {
        /// <summary>
        /// Преобразует данные объекта в фильтр для запроса в MongoDB.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="model">Модель объекта.</param>
        /// <returns>Возвращает фильтр для поиска.</returns>
        public static FilterDefinition<TModel> ToMongoFilter<TModel>(this TModel model)
        {
            if (model == null)
            {
                return FilterDefinition<TModel>.Empty;
            }

            var type = typeof(TModel);
            var props = type.GetProperties();
            var queryParameters = props
                                    .Select(property =>
                                    {
                                        var data = property.GetMethod.Invoke(model, null);
                                        return data.IsDefault()
                                        ? null
                                        : Builders<TModel>.Filter.Eq(property.Name, data);
                                    })
                                    .Where(parameter => parameter != null)
                                    .ToArray();
            return !queryParameters.Any()
                ? FilterDefinition<TModel>.Empty
                : queryParameters.Aggregate((definition, filterDefinition) => definition & filterDefinition);
        }

        /// <summary>
        /// Преобразует данные объекта в фильтр для запроса в MongoDB.
        /// </summary>
        /// <typeparam name="TModel">Тип модели.</typeparam>
        /// <param name="model">Модель объекта.</param>
        /// <returns>Возвращает фильтр для поиска.</returns>
        public static UpdateDefinition<TModel> ToMongoUpdateFilter<TModel>(this TModel model) where TModel : IMongoModel
        {
            if (model == null)
            {
                return null;
            }

            var type = typeof(TModel);
            var props = type.GetProperties();
            var queryParameters = props
                                    .Select(property =>
                                    {
                                        if (property.Name == nameof(model.Id))
                                        {
                                            return null;
                                        }

                                        var data = property.GetMethod.Invoke(model, null);
                                        return Builders<TModel>.Update.Set(property.Name, data);
                                    })
                                    .Where(parameter => parameter != null)
                                    .ToArray();
            return !queryParameters.Any()
                ? null
                : Builders<TModel>.Update.Combine(queryParameters);
        }

        /// <summary>
        /// Определяет является ли значение объекта значением по умолчанию.
        /// </summary>
        /// <typeparam name="T">Тип объекта.</typeparam>
        /// <param name="o">Объект.</param>
        /// <returns>Возвращает true, если значение объекта является значением по умолчанию.</returns>
        public static bool IsDefault<T>(this T o)
        {
            if (o == null) // ссылочный тип или nullable
                return true;
            if (Nullable.GetUnderlyingType(typeof(T)) != null) // nullable, не null
                return false;
            var type = o.GetType();
            if (type.IsClass)
                return false;
            else           // тип-значение, есть конструктор по умолчанию
                return Activator.CreateInstance(type).Equals(o);
        }
    }
}
