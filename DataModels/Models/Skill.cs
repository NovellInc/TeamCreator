using DataModels.Enums;

namespace DataModels.Models
{
    /// <summary>
    /// Навык.
    /// </summary>
    public class Skill
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Skill"/>.
        /// </summary>
        /// <param name="kindOfSport">Вид спорта.</param>
        public Skill(KindOfSport kindOfSport)
        {
            KindOfSport = kindOfSport;
        }

        /// <summary>
        /// Получает вид спорта.
        /// </summary>
        public KindOfSport KindOfSport { get; set; }

        /// <summary>
        /// Получает или задает уровень навыка Нападение.
        /// </summary>
        public double Forward { get; set; }

        /// <summary>
        /// Получает или задает уровень навыка Полузащита.
        /// </summary>
        public double Halfback { get; set; }

        /// <summary>
        /// Получает или задает уровень навыка Защита.
        /// </summary>
        public double Defender { get; set; }

        /// <summary>
        /// Получает или задает уровень навыка Вратарь.
        /// </summary>
        public double Goalkeeper { get; set; }
    }
}
