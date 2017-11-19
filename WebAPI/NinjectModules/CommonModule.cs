using System;
using System.Configuration;
using Dal.Interfaces;
using Dal.Repositories;
using MongoDB.Driver;
using Ninject;
using Ninject.Modules;
using NLog;
using WebAPI.Services;

namespace WebAPI.NinjectModules
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
                this.Bind<IMongoRepository>()
                    .To<MongoRepository>()
                    .InSingletonScope()
                    .WithConstructorArgument("mongoUrl", mongoUrl);
                this.Bind<RestService>()
                    .ToSelf()
                    .InSingletonScope()
                    .WithConstructorArgument("baseAddress", ConfigurationManager.AppSettings["baseAddress"])
                    .WithConstructorArgument("mongoRepository", this.Kernel.Get<IMongoRepository>());
            }
            catch (Exception e)
            {
                Log.Fatal($"При инициализации сервиса произошла ошибка: {e}.");
            }
        }
    }
}
