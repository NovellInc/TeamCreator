using System;
using System.Web.Http;
using System.Web.Http.Description;
using Dal.Extensions;
using Dal.Interfaces;
using DataModels.Interfaces;
using DataModels.Models;
using MongoDB.Bson;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Контроллер работы с Играми.
    /// </summary>
    [RoutePrefix("api")]
    public sealed class GamesController : ApiController
    {
        /// <summary>
        /// Хранилище.
        /// </summary>
        private readonly IMongoRepository _mongoRepository;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="GamesController"/>.
        /// </summary>
        /// <param name="mongoRepository">Хранилище.</param>
        public GamesController(IMongoRepository mongoRepository)
        {
            this._mongoRepository = mongoRepository;
        }

        /// <summary>
        /// Получает Игры согласно фильтру.
        /// </summary>
        /// <param name="game">Фильтр.</param>
        /// <returns>Возвращает постраничный список Игр.</returns>
        [HttpGet]
        [ResponseType(typeof(IPagedList<Game>))]
        [Route("games")]
        public IHttpActionResult GetGames([FromUri] Game game)
        {
            try
            {
                return this.Ok(this._mongoRepository.Get(game.ToMongoFilter()));
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }

        /// <summary>
        /// Получает игру по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор.</param>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(Game))]
        [Route("game/{id}")]
        public IHttpActionResult GetGame([FromUri] ObjectId id)
        {
            try
            {
                return this.Ok(this._mongoRepository.Get<Game>(id));
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }

        /// <summary>
        /// Создаёт Игру.
        /// </summary>
        /// <param name="game">Игра.</param>
        /// <returns>Возвращает идентификатор Игры в случае успешного создания.</returns>
        [HttpPost]
        [ResponseType(typeof(ObjectId))]
        [Route("game")]
        public IHttpActionResult AddGame([FromBody] Game game)
        {
            try
            {
                return this.Ok(this._mongoRepository.Add(game));
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }

        /// <summary>
        /// Удаляет Игру.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("game")]
        public IHttpActionResult DeleteGame([FromBody] ObjectId id)
        {
            try
            {
                this._mongoRepository.Delete<Game>(id);
                return this.Ok();
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }
    }
}
