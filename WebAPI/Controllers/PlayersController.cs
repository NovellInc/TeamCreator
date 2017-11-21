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
    /// Контроллер работы с игроками.
    /// </summary>
    [RoutePrefix("api")]
    public sealed class PlayersController : ApiController, ICrudController<Player>
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
        public IHttpActionResult Get([FromUri] Player player)
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
        /// <returns>Возвращает игрока.</returns>
        [HttpGet]
        [ResponseType(typeof(Player))]
        [Route("player/{id}")]
        public IHttpActionResult Get([FromUri] string id)
        {
            try
            {
                return this.Ok(this._mongoRepository.Get<Player>(ObjectId.Parse(id)));
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
        public IHttpActionResult Create([FromBody] Player player)
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
        /// Обновляет данные об игроке.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <returns></returns>
        [HttpPut]
        [Route("player")]
        public IHttpActionResult Update([FromBody] Player player)
        {
            if (player.Id.IsDefault())
            {
                return this.BadRequest();
            }

            try
            {
                this._mongoRepository.Update(player);
                return this.StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception)
            {
                return this.BadRequest();
            }
        }

        public IHttpActionResult Delete(string id, string creatorId)
        {
            return this.NotFound();
        }
    }
}
