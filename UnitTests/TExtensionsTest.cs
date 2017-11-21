using Dal.Extensions;
using DataModels.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace UnitTests
{
    [TestClass]
    public class TExtensionsTest
    {
        [TestMethod]
        public void ToMongoFilterTest()
        {
            var player = new Player(2345432)
            {
                Name = "Name"
            };
            var filter = player.ToMongoFilter();
            var expected = Builders<Player>.Filter.Eq(nameof(Player.TelegramId), (object)player.TelegramId) &
                           Builders<Player>.Filter.Eq(nameof(Player.Name), (object)player.Name);
            Assert.AreEqual(filter, expected);
        }

        [TestMethod]
        public void ExtractCommandParamsTest()
        {
            var game = new Game(ObjectId.Parse("5a08638706a6387034f5f67e"), false);
            var mongoUrl = new MongoUrl("mongodb://localhost:27017/TeamCreator");
            var filter = Builders<Game>.Filter.Eq("_id", game.Id);
            //var updateDefinition = Builders<Game>.Update;
            var update = game.ToMongoUpdateFilter();
            new MongoClient(mongoUrl).GetDatabase(mongoUrl.DatabaseName).GetCollection<Game>(typeof(Game).Name).UpdateOne(filter, update);
        }
    }
}
