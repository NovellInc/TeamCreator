using System.ComponentModel;

namespace TelegramBot.Models
{
    /// <summary>
    /// Часовые пояса.
    /// </summary>
    public enum TimeZoneOffset
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("+2 Калининград")]
        Калининград = 2,

        /// <summary>
        /// 
        /// </summary>
        [Description("+3 Москва")]
        Москва = 3,

        /// <summary>
        /// 
        /// </summary>
        [Description("+5 Екатеринбург, Уфа")]
        Екатеринбург = 5
    }
}
