using System;
using Newtonsoft.Json;
using TelegramBot.Models;

namespace TelegramBot.Extensions
{
    /// <summary>
    /// Класс расширений для строк.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Извлекает параметры команды.
        /// </summary>
        /// <typeparam name="TModel">Тип модели параметров.</typeparam>
        /// <param name="rawData">Сырые данные.</param>
        /// <param name="command">Команда.</param>
        /// <param name="model">Объект модели параметров.</param>
        /// <returns></returns>
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
        /// Разделяет данные на команду и параметры.
        /// </summary>
        /// <param name="rawData">Данные.</param>
        /// <param name="splitter">Разделитель данных.</param>
        /// <param name="param">Объект модели параметров.</param>
        /// <returns>Возвращает команду.</returns>
        public static CallbackBotCommands SplitCommandAndParams(this string rawData, char splitter, out string param)
        {
            try
            {
                var splitterIndex = rawData.IndexOf(splitter);
                if (splitterIndex > 0 && rawData.Length > splitterIndex + 1)
                {
                    param = rawData.Substring(splitterIndex + 1);
                    return (CallbackBotCommands) Enum.Parse(typeof(CallbackBotCommands), rawData.Remove(splitterIndex));
                }

                param = string.Empty;
                return (CallbackBotCommands)Enum.Parse(typeof(CallbackBotCommands), rawData.TrimEnd(splitter));
            }
            catch (Exception)
            {
                // No action
            }

            param = string.Empty;
            return CallbackBotCommands.UnknownCommand;
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
