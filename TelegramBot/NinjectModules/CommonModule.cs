using System;
using System.Configuration;
using DataModels.Interfaces;
using DataModels.Models;
using MongoDB.Driver;
using Ninject.Modules;
using NLog;
using TelegramBot.Services;

namespace TelegramBot.NinjectModules
{
    /// <summary>
    /// Класс определяет привязки в приложении.
    /// </summary>
    public sealed class CommonModule : NinjectModule
    {
        /// <summary>
        /// Журнал событий.
        /// </summary>
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Загружает информацию в ядро.
        /// </summary>
        public override void Load()
        {
            try
            {
                var mongoUrl = new MongoUrl(ConfigurationManager.ConnectionStrings["MongoUrl"].ConnectionString);
                this.Bind<IRepository>()
                    .To<MongoRepository>()
                    .InSingletonScope()
                    .WithConstructorArgument("mongoUrl", mongoUrl);
                this.Bind<TelegramBotService>()
                    .ToSelf()
                    .InSingletonScope()
                    .WithConstructorArgument("token", ConfigurationManager.AppSettings["TelegramBotToken"]);
            }
            catch (Exception e)
            {
                Log.Fatal($"При инициализации сервиса произошла ошибка: {e}.");
            }
        }
    }
}
