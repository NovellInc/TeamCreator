using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ApiClient.Exceptions;
using ApiClient.Extensions;
using ApiClient.Models;
using DataModels.Extensions;
using DataModels.Models;
using Newtonsoft.Json;
using RestSharp;

namespace ApiClient
{
    /// <summary>
    /// Класс предоставляет инструменты для доступа к API реестра сертификатов.
    /// </summary>
    public class ApiClient : IApiClient
    {
        /// <summary>
        /// Словарь URL-префиксов.
        /// </summary>
        public static Dictionary<Route, string> RoutePrefixes;

        /// <summary>
        /// Инициализирует статичные поля класса.
        /// </summary>
        static ApiClient()
        {
            RoutePrefixes = Enum.GetValues(typeof (Route))
                                .Cast<Route>()
                                .ToDictionary(item => item, item => item.GetElementDescription());
        }
        
        /// <summary>
        /// API URL.
        /// </summary>
        private readonly string _baseApiUrl;

        /// <summary>
        /// Куки.
        /// </summary>
        private CookieContainer _cookieContainer;

        /// <summary>
        /// Инициализирует ноывй экземпляр класса <see cref="ApiClient"/>.
        /// </summary>
        /// <param name="reestrApiUrl">API URL.</param>
        /// <param name="loginModel">Модель для авторизации пользователя.</param>
        public ApiClient(string reestrApiUrl, LoginModel loginModel)
        {
            if (string.IsNullOrEmpty(reestrApiUrl))
            {
                throw new ArgumentNullException(nameof(reestrApiUrl), "Строка адреса не может быть пустой");
            }

            this._baseApiUrl = reestrApiUrl.TrimEnd('/', ' ');
            if (loginModel == null)
            {
                throw new ArgumentNullException(nameof(loginModel), "Отсутствуют данные для авторизации");
            }

            this.LoginModel = loginModel;

            this.Login();
        }
        
        /// <summary>
        /// Модель для авторизации пользователя.
        /// </summary>
        public LoginModel LoginModel { get; private set; }
        
        /// <summary>
        /// Авторизует пользователя для доступа к API.
        /// </summary>
        /// <exception cref="AuthorizeException"></exception>
        /// <param name="loginModel">Модель данных для авторизации.</param>
        /// <returns>Возвращает данные авторизованного пользователя.</returns>
        public SystemUser Login(LoginModel loginModel = null)
        {
            if (loginModel != null)
            {
                this.LoginModel = loginModel;
            }

            this._cookieContainer = null;
            var user = this.ExecuteRequest<SystemUser>(this.CreateUrl(Route.Login), Method.POST, this.LoginModel);
            if (user == null)
            {
                throw new AuthorizeException("Ошибка авторизации: пользователь не авторизован");
            }
            
            return user;
        }

        /// <summary>
        /// Производит выход пользователя из API.
        /// </summary>
        public void Logout()
        {
            var client = new RestClient(this.CreateUrl(Route.Logout));
            var request = new RestRequest(Method.GET);
            client.Execute(request);
        }
        
        /// <summary>
        /// Выполняет типизированный запрос.
        /// </summary>
        /// <typeparam name="TResponseModel">Тип ответа.</typeparam>
        /// <param name="url">Строка запроса.</param>
        /// <param name="method">Метод запроса.</param>
        /// <param name="model">Данные для POST запроса.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AuthorizeException"></exception>
        /// <exception cref="Exception"></exception>
        /// <returns>Возвращает ответ типа <see cref="TResponseModel"/>.</returns>
        private TResponseModel ExecuteRequest<TResponseModel>(string url, Method method = Method.GET, object model = null) where TResponseModel : new() 
        {
            var client = new RestClient(url);
            var request = new RestRequest(method);
            if (method == Method.POST)
            {
                if (model == null)
                {
                    throw new ArgumentNullException(nameof(model), "Отсутствуют данные для POST запроса");
                }
                
                request.AddParameter("application/json", JsonConvert.SerializeObject(model), ParameterType.RequestBody);
            }

            if (this._cookieContainer != null)
            {
                client.CookieContainer = this._cookieContainer;
            }

            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new AuthorizeException(response.Content);
            }

            string setCookieHeader = response.Headers.FirstOrDefault(header => header.Name == "Set-Cookie")?.Value.ToString();
            if (!string.IsNullOrEmpty(setCookieHeader))
            {
                this._cookieContainer = new CookieContainer();
                this._cookieContainer.SetCookies(new Uri(url), setCookieHeader);
            }

            var responseModel = JsonConvert.DeserializeObject<TResponseModel>(response.Content);
            if (responseModel != null)
            {
                return responseModel;
            }

            if (response.ErrorException != null)
            {
                throw response.ErrorException;
            }

            return default(TResponseModel);
        }

        /// <summary>
        /// Получает объект <see cref="TResponseModel"/> от API.
        /// </summary>
        /// <param name="filter">Модель фильтра данных</param>
        /// <exception cref="AuthorizeException"></exception>
        /// <exception cref="Exception"></exception>
        /// <returns>Возвращает объект <see cref="TResponseModel"/> от API.</returns>
        public TResponseModel Get<TResponseModel, TFilter>(TFilter filter) where TResponseModel : new()
        {
            Route route;
            if (filter is Player)
            {
                route = Route.Players;
            }
            else if(filter is Game)
            {
                route = Route.Games;
            }
            else if (filter is SportGround)
            {
                route = Route.SportGrounds;
            }
            else
            {
                throw new Exception($"Невозможно определить маршрут запроса по типу фильтра: {typeof(TFilter).FullName}");
            }

            string requestString = this.CreateUrl(route, filter);
            try
            {
                return this.ExecuteRequest<TResponseModel>(requestString);
            }
            catch (AuthorizeException)
            {
                this.Login();
                return this.ExecuteRequest<TResponseModel>(requestString);
            }
        }

        /// <summary>
        /// Создаёт URL запроса.
        /// </summary>
        /// <param name="routePrefix">Префикс команды запроса.</param>
        /// <returns>Возвращает url строку.</returns>
        private string CreateUrl(Route routePrefix)
        {
            return string.Concat(this._baseApiUrl, "/", RoutePrefixes[routePrefix]);
        }

        /// <summary>
        /// Создаёт URL запроса.
        /// </summary>
        /// <param name="routePrefix">Префикс команды запроса.</param>
        /// <param name="filter">Фильтр.</param>
        /// <returns>Возвращает url строку.</returns>
        private string CreateUrl<TFilter>(Route routePrefix, TFilter filter)
        {
            return string.Concat(this._baseApiUrl, "/", RoutePrefixes[routePrefix], filter.ToQueryGetParameters());
        }
    }
}
