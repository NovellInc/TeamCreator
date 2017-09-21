using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using MongoDB.Driver;

namespace DataModels.Extensions
{
    /// <summary>
    /// Класс содержит обобщенные методы расширения. 
    /// </summary>
    public static class TExtensions
    {
        /// <summary>
        /// Получает значение атрибута. 
        /// </summary>
        /// <typeparam name="TType">Тип атрибута.</typeparam>
        /// <typeparam name="TReturn">Тип возвращаемого значения.</typeparam>
        /// <param name="assembly">Сборка.</param>
        /// <param name="func">Функция для получения значения из атрибута.</param>
        /// <returns>
        /// Возвращает значение атрибута.
        /// </returns>
        public static TReturn GetAttributeValue<TType, TReturn>(this Assembly assembly, Func<TType, TReturn> func) where TType : Attribute
        {
            Type attributeType = typeof(TType);
            if (!Attribute.IsDefined(assembly, attributeType))
                return default(TReturn);
            TType type = (TType)Attribute.GetCustomAttribute(assembly, attributeType);
            return func(type);
        }

        /// <summary>
        /// Получает описание свойства.
        /// </summary>
        /// <typeparam name="T">Тип объекта.</typeparam>
        /// <param name="obj">Объект для получения описания.</param>
        /// <returns>Возвращает описание.</returns>
        public static string GetElementDescription<T>(this T obj)
        {
            var type = typeof(T);
            Type nullableType;
            if ((nullableType = Nullable.GetUnderlyingType(type)) != null)
            {
                type = nullableType;
            }

            FieldInfo fieldInfo = type.GetField(obj.ToString());
            DescriptionAttribute descriptionAttribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>(false);
            return descriptionAttribute == null ? string.Empty : descriptionAttribute.Description;
        }

        /// <summary>
        /// Получает описание класса.
        /// </summary>
        /// <typeparam name="T">Тип объекта.</typeparam>
        /// <param name="obj">>Объект для получения описания.</param>
        /// <returns>Возвращает описание.</returns>
        public static string GetClassDescription<T>(this T obj)
        {
            DescriptionAttribute descriptionAttribute;
            try
            {
                descriptionAttribute = obj.GetType().GetCustomAttribute<DescriptionAttribute>();
            }
            catch (Exception)
            {
                return string.Empty;
            }

            return descriptionAttribute != null ? descriptionAttribute.Description : string.Empty;
        }

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
