using ApiClient.Models;

namespace ApiClient
{
    /// <summary>
    /// Интерфейс работы с API реестра сертификатов.
    /// </summary>
    public interface IApiClient
    {
        /// <summary>
        /// Позволяет произвести авторизацию в API.
        /// </summary>
        /// <param name="loginModel">Модель данных для авторизации.</param>
        /// <returns>Возвращает объект <see cref="SystemUser"/> от API.</returns>
        SystemUser Login(LoginModel loginModel);

        /// <summary>
        /// Производит выход пользователя из системы.
        /// </summary>
        void Logout();

        /// <summary>
        /// Получает объект <see cref="TResponseModel"/> от API.
        /// </summary>
        /// <typeparam name="TResponseModel">Тип ответа.</typeparam>
        /// <typeparam name="TFilter">Тип фильтра.</typeparam>
        /// <param name="filter">Модель фильтра данных</param>
        /// <returns>Возвращает объект <see cref="TResponseModel"/> от API.</returns>
        TResponseModel Get<TResponseModel, TFilter>(TFilter filter) where TResponseModel : new();
    }
}
