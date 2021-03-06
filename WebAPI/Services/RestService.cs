﻿using System;
using System.Web.Http;
using System.Web.Http.SelfHost;
using Dal.Interfaces;
using Ninject.Web.WebApi;
using NLog;
using WebAPI.NinjectModules;

namespace WebAPI.Services
{
    /// <summary>
    /// Сервис REST API.
    /// </summary>
    public class RestService : IDisposable
    {
        /// <summary>
        /// Журнал событий.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Адрес сервера.
        /// </summary>
        private readonly string _baseAddress;

        /// <summary>
        /// HTTP сервер.
        /// </summary>
        private readonly HttpSelfHostServer _server;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RestService"/>.
        /// </summary>
        /// <param name="baseAddress">Адрес, по которому доступен API.</param>
        /// <param name="mongoRepository">Хранилище.</param>
        public RestService(string baseAddress, IMongoRepository mongoRepository)
        {
            if (baseAddress == null)
            {
                throw new ArgumentNullException(nameof(baseAddress),
                                                "Необходимо указать базовый адрес REST API сервиса");
            }

            this._baseAddress = baseAddress;
            var selfHostConfiguraiton = new HttpSelfHostConfiguration(new Uri(this._baseAddress));
            selfHostConfiguraiton.MapHttpAttributeRoutes();
            selfHostConfiguraiton.DependencyResolver = new NinjectDependencyResolver(CustomWebApiModule.CreateKernel(selfHostConfiguraiton, mongoRepository));
            selfHostConfiguraiton.EnsureInitialized();
            this._server = new HttpSelfHostServer(selfHostConfiguraiton);
        }

        /// <summary>
        /// Запускает сервис.
        /// </summary>
        public void Start()
        {
            this._server.OpenAsync();
            Logger.Info($"Сервис REST API по адресу \"{this._baseAddress}\" запущен.");
        }


        /// <summary>
        /// Останавливает сервис.
        /// </summary>
        public void Stop()
        {
            this._server.CloseAsync();
            Logger.Info($"Сервис REST API по адресу \"{this._baseAddress}\" остановлен.");
        }

        /// <summary>
        /// Освобождает ресурсы.
        /// </summary>
        public void Dispose()
        {
            this._server?.Dispose();
        }
    }
}
