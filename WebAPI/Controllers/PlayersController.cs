using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;
using DataModels.Models;

namespace WebAPI.Controllers
{
    [RoutePrefix("api")]
    public class PlayersController : ApiController
    {
        [HttpGet]
        [ResponseType(typeof(IEnumerable<Player>))]
        [Route("players")]
        public IHttpActionResult GetPlayers()
        {
            return this.Ok();
        }

        [HttpGet]
        [ResponseType(typeof(Player))]
        [Route("player/{id}")]
        public IHttpActionResult GetPlayer()
        {
            return this.Ok();
        }
    }
}
