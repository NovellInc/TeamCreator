using System.ComponentModel;

namespace TelegramBot.Resources
{
    /// <summary>
    /// Часовые пояса.
    /// </summary>
    public enum TimeZone
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
