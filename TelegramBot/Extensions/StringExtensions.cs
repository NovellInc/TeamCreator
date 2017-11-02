using System;
using Newtonsoft.Json;

namespace TelegramBot.Extensions
{
    /// <summary>
    /// Класс расширений для строк.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Пытается извлечь параметры команды.
        /// </summary>
        /// <typeparam name="TModel">Тип модели параметров.</typeparam>
        /// <param name="rawData">Сырые данные.</param>
        /// <param name="command">Команда.</param>
        /// <param name="model">Объект модели параметров.</param>
        /// <returns>true, если параметры .</returns>
        public static bool ExtractCommandParams<TModel>(this string rawData, string command, out TModel model)
        {
            try
            {
                if (rawData.StartsWith(command, StringComparison.OrdinalIgnoreCase))
                {
                    string dataParams = rawData.Replace(command, string.Empty).Trim();
                    model = string.IsNullOrEmpty(dataParams)
                        ? default(TModel)
                        : dataParams.FromJson<TModel>();
                    return true;
                }
            }
            catch (Exception)
            {
                // No action
            }

            model = default(TModel);
            return false;
        }

        /// <summary>
        /// Сериализует объект в JSON. Все члены объекта равные null или значению по умолчанию игнорируются.
        /// </summary>
        /// <typeparam name="TModel">Тип объекта.</typeparam>
        /// <param name="model">Объект.</param>
        /// <returns>Возвращает сериализованную строку.</returns>
        public static string ToJson<TModel>(this TModel model)
        {
            return JsonConvert.SerializeObject(model, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
        }

        /// <summary>
        /// Десериализует строку JSON в объект.
        /// </summary>
        /// <typeparam name="TModel">Тип объекта.</typeparam>
        /// <param name="model">Объект.</param>
        /// <returns>Возвращает десериализованный объект.</returns>
        public static TModel FromJson<TModel>(this string model)
        {
            return JsonConvert.DeserializeObject<TModel>(model);
        }
    }
}
