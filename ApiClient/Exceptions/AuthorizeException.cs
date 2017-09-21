using System;

namespace ApiClient.Exceptions
{
    /// <summary>
    /// Ошибка возникает при отсутствии авторизации пользователя.
    /// </summary>
    public class AuthorizeException : Exception
    {
        public AuthorizeException(string message) : base(message)
        {
        }

        public AuthorizeException() : base("Ошибка авторизации")
        {
        }
    }
}
