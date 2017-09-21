using System;
using DataModels.Models;

namespace ApiClient.Models
{
    /// <summary>
    /// Класс пользователь системы.
    /// </summary>
    public sealed class SystemUser : User
    {
        /// <summary>
        /// Получает или задает идетификатор пользователя.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Получает или задает роль пользователя.
        /// </summary>
        public UserRole Role { get; set; }
    }
}
