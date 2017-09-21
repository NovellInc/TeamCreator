using System.ComponentModel;

namespace ApiClient.Models
{
    /// <summary>
    /// Маршрут URL запроса.
    /// </summary>
    public enum Route
    {
        /// <summary>
        /// Авторизация пользователя.
        /// </summary>
        [Description("account/login")]
        Login,

        /// <summary>
        /// Получение текущего авторизованного пользователя.
        /// </summary>
        [Description("account/current")]
        Current,

        /// <summary>
        /// Выход пользователя из системы.
        /// </summary>
        [Description("account/logout")]
        Logout,

        /// <summary>
        /// Получение игроков.
        /// </summary>
        [Description("players")]
        Players,

        /// <summary>
        /// Получение игр.
        /// </summary>
        [Description("games")]
        Games,
        
        /// <summary>
        /// Получение игровых площадок.
        /// </summary>
        [Description("sportgrounds")]
        SportGrounds
    }
}
