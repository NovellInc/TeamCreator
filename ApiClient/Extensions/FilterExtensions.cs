using System.Linq;

namespace ApiClient.Extensions
{
    /// <summary>
    /// Класс расширений для фильтров.
    /// </summary>
    public static class FilterExtensions
    {
        /// <summary>
        /// Конвертирует фильтр в строку для GET запроса.
        /// </summary>
        /// <typeparam name="TFilterModel">Модель фильтра.</typeparam>
        /// <param name="filter">Фильтр.</param>
        /// <returns>Возвращает строку для GET запроса.</returns>
        public static string ToQueryGetParameters<TFilterModel>(this TFilterModel filter)
        {
            if (filter == null)
            {
                return string.Empty;
            }

            var type = typeof(TFilterModel);
            var props = type.GetProperties();
            var queryParameters = props
                                    .Select(property =>
                                    {
                                        var data = property.GetMethod.Invoke(filter, null);
                                        return data == null ? null : $"{property.Name}={data}";
                                    })
                                    .Where(parameter => !string.IsNullOrEmpty(parameter))
                                    .ToArray();
            return !queryParameters.Any() ? string.Empty : $"?{string.Join("&", queryParameters)}";
        }
    }
}
