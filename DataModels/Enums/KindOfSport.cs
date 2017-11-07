using System.ComponentModel;

namespace DataModels.Enums
{
    /// <summary>
    /// Вид спорта.
    /// </summary>
    public enum KindOfSport
    {
        /// <summary>
        /// Не указан.
        /// </summary>
        [Description("Не указан")]
        Default = 0,

        /// <summary>
        /// Футбол.
        /// </summary>
        [Description("Футбол")]
        Football = 1,

        /// <summary>
        /// Футзал.
        /// </summary>
        [Description("Футзал")]
        Futsal = 2
    }
}
