using System.Web.Http;
using System.Web.Http.Description;
using Dal.Extensions;
using Dal.Interfaces;
using DataModels.Interfaces;
using DataModels.Models;
using WebAPI.Filters;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Контроллер доступа к Играм.
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
        /// <returns>Возвращает список Игр.</returns>
        [HttpGet]
        [ResponseType(typeof(IPagedList<Game>))]
        [Route("games")]
        public IHttpActionResult GetGames([FromUri] Game game)
        {
            return this.Ok(this._mongoRepository.Get(game.ToMongoFilter()));
        }
    }
}
