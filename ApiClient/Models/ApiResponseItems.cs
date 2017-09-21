namespace ApiClient.Models
{
    /// <summary>
    /// Массив данных из ответа API.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public class ApiResponseItems<TModel>
    {
        /// <summary>
        /// Список данных типа <see cref="TModel"/>.
        /// </summary>
        public TModel[] Items { get; set; }
    }
}
