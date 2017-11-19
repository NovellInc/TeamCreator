using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Validation;
using Dal.Interfaces;
using Ninject;
using Ninject.Web.WebApi.Filter;
using NLog;

namespace WebAPI.NinjectModules
{
    /// <summary>
    /// Модуль ядра для WebApi.
    /// </summary>
    public static class CustomWebApiModule
    {
        /// <summary>
        /// Журнал.
        /// </summary>
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Создает ядро.
        /// </summary>
        /// <param name="configuration">Настройки HTTP.</param>
        /// <param name="mongoRepository">Хранилище.</param>
        /// <returns>Возвращает созданное ядро.</returns>
        public static IKernel CreateKernel(HttpConfiguration configuration, IMongoRepository mongoRepository)
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<DefaultModelValidatorProviders>()
                      .ToConstant(new DefaultModelValidatorProviders(configuration.Services
                                                                                  .GetServices(typeof(ModelValidatorProvider))
                                                                                  .Cast<ModelValidatorProvider>()));
                kernel.Bind<DefaultFilterProviders>()
                      .ToConstant(new DefaultFilterProviders(configuration.Filters.Cast<DefaultFilterProvider>()));

                kernel.Bind<IMongoRepository>().ToConstant(mongoRepository);

                return kernel;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Ошибка при инициализации ядра WebAPI: {e}");
                throw;
            }
        }
    }
}
