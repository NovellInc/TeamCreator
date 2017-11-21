using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Dal.Interfaces;
using DataModels.Interfaces;
using DataModels.Models;
using MongoDB.Bson;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Контроллер работы с командами.
    /// </summary>
    [RoutePrefix("api")]
    public sealed class TeamsController : ApiController, ICrudController<Team>
    {
        /// <summary>
        /// Хранилище.
        /// </summary>
        private readonly IMongoRepository _mongoRepository;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TeamsController"/>.
        /// </summary>
        /// <param name="mongoRepository">Хранилище.</param>
        public TeamsController(IMongoRepository mongoRepository)
        {
            _mongoRepository = mongoRepository;
        }

        [HttpGet]
        [ResponseType(typeof(IPagedList<Team>))]
        [Route("teams")]
        public IHttpActionResult Get([FromUri] Team team)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [ResponseType(typeof(Team))]
        [Route("teams/{id}")]
        public IHttpActionResult Get([FromUri] string id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ResponseType(typeof(ObjectId))]
        [Route("team")]
        public IHttpActionResult Create([FromBody] Team team)
        {
            throw new NotImplementedException();
        }

        [HttpPut]
        [Route("team")]
        public IHttpActionResult Update([FromBody] Team team)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("team/{id}")]
        public IHttpActionResult Delete(string id, [FromBody] string captainId)
        {
            throw new NotImplementedException();
        }
    }
}
