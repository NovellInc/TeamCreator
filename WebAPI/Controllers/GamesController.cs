using System;
using System.Web.Http;
using System.Web.Http.Description;
using Dal.Extensions;
using Dal.Interfaces;
using DataModels.Interfaces;
using DataModels.Models;
using Extensions;
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
        public IHttpActionResult GetGame([FromUri] string id)
        {
            try
            {
                return this.Ok(this._mongoRepository.Get<Game>(ObjectId.Parse(id)));
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
        /// Обновляет данные об Игре.
        /// </summary>
        /// <param name="game">Игра.</param>
        /// <returns></returns>
        [HttpPut]
        [Route("game")]
        public IHttpActionResult UpdateGame([FromBody] Game game)
        {
            if (game.Id.IsDefault())
            {
                return this.BadRequest();
            }

            try
            {
                this._mongoRepository.Update(game);
                return this.Ok();
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }

        /// <summary>
        /// Удаляет Игру.
        /// </summary>
        /// <param name="id">Идентификатор Игры.</param>
        /// <param name="creatorId">Идентификатор создателя Игры.</param>
        /// <returns></returns>
        [HttpPost]
        [Route("game/{id}")]
        public IHttpActionResult DeleteGame(string id, [FromBody] string creatorId)
        {
            try
            {
                this._mongoRepository.Delete<Game>(ObjectId.Parse(id));
                return this.Ok();
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }
    }
}
