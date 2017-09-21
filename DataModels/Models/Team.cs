using System.Collections.Generic;

namespace DataModels.Models
{
    /// <summary>
    /// Класс описывает команду.
    /// </summary>
    public class Team
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Team"/>.
        /// </summary>
        public Team()
        {
            this.Players = new HashSet<Player>();
        }

        /// <summary>
        /// Получает или задает название команды.
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// Получает или задает игроков команды.
        /// </summary>
        public HashSet<Player> Players { get; set; }

        /// <summary>
        /// Капитан команды.
        /// </summary>
        public Player Captain { get; set; }
    }
}
