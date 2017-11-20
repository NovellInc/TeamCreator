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
    /// Контроллер работы с игроками.
    /// </summary>
    [RoutePrefix("api")]
    public sealed class PlayersController : ApiController
    {
        /// <summary>
        /// Хранилище.
        /// </summary>
        private readonly IMongoRepository _mongoRepository;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PlayersController"/>.
        /// </summary>
        /// <param name="mongoRepository">Хранилище.</param>
        public PlayersController(IMongoRepository mongoRepository)
        {
            _mongoRepository = mongoRepository;
        }

        /// <summary>
        /// Получает игроков согласно фильтру.
        /// </summary>
        /// <param name="player">Фильтр.</param>
        /// <returns>Возвращает постраничный список игроков.</returns>
        [HttpGet]
        [ResponseType(typeof(IPagedList<Player>))]
        [Route("players")]
        public IHttpActionResult GetPlayers([FromUri] Player player)
        {
            try
            {
                return this.Ok(this._mongoRepository.Get(player.ToMongoFilter()));
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
        [ResponseType(typeof(Player))]
        [Route("player/{id}")]
        public IHttpActionResult GetPlayer([FromUri] ObjectId id)
        {
            try
            {
                return this.Ok(this._mongoRepository.Get<Player>(id));
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }
        
        /// <summary>
        /// Создаёт игрока.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <returns>Возвращает идентификатор игрока в случае успешного создания.</returns>
        [HttpPost]
        [ResponseType(typeof(ObjectId))]
        [Route("player")]
        public IHttpActionResult AddPlayer([FromBody] Player player)
        {
            try
            {
                return this.Ok(this._mongoRepository.Add(player));
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }

        /// <summary>
        /// Удаляет игрока.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("player")]
        public IHttpActionResult DeletePlayer([FromBody] ObjectId id)
        {
            try
            {
                this._mongoRepository.Delete<Player>(id);
                return this.Ok();
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }
    }
}
