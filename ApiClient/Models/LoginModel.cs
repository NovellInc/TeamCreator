using System;

namespace ApiClient.Models
{
    /// <summary>
    /// Модель для авторизации пользователя.
    /// </summary>
    public class LoginModel
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="LoginModel"/>.
        /// </summary>
        /// <param name="userName">Имя пользователя.</param>
        /// <param name="password">Пароль.</param>
        /// <param name="rememberMe">Флажок "запомнить меня".</param>
        public LoginModel(string userName, string password, bool rememberMe = false)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName),"Имя пользователя не может быть пустым");
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password), "Пароль не может быть пустым");
            }

            UserName = userName;
            Password = password;
            RememberMe = rememberMe;
        }

        /// <summary>
        /// Получает или задает имя пользователя.
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// Получает или задает пароль пользователя.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Получает или задает признак постоянного хранения данных авторизации.
        /// </summary>
        public bool RememberMe { get; }
    }
}
