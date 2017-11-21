using System;
using System.Net;
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
    public sealed class GamesController : ApiController, ICrudController<Game>
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
        public IHttpActionResult Get([FromUri] Game game)
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
        [Route("games/{id}")]
        public IHttpActionResult Get([FromUri] string id)
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
        public IHttpActionResult Create([FromBody] Game game)
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
        /// <returns>Возвращает Игру.</returns>
        [HttpPut]
        [Route("game")]
        public IHttpActionResult Update([FromBody] Game game)
        {
            if (game.Id.IsDefault())
            {
                return this.BadRequest();
            }

            try
            {
                this._mongoRepository.Update(game);
                return this.StatusCode(HttpStatusCode.NoContent);
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
        public IHttpActionResult Delete(string id, [FromBody] string creatorId)
        {
            try
            {
                var gameId = ObjectId.Parse(id);
                if (this._mongoRepository.Get<Game>(gameId).CreatorId.ToString() != creatorId)
                {
                    throw new ArgumentException("Удалять Игру может только создатель Игры", nameof(creatorId));
                }

                this._mongoRepository.Delete<Game>(gameId);
                return this.StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }
    }
}
