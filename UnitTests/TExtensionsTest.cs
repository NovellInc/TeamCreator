using DataModels.Enums;
using DataModels.Extensions;
using DataModels.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using TelegramBot.Extensions;
using TelegramBot.Models;

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
            var game = new Game(ObjectId.Parse("5a041e8206a63808cc0ea74e"), true);
            var mongoUrl = new MongoUrl("mongodb://localhost:27017/TeamCreator");
            var filter = Builders<Game>.Filter.Eq("_id", game.Id);
            var updateDefinition = Builders<Game>.Update;
            var update = updateDefinition.Combine(updateDefinition.Set(nameof(game.Name), game.Name), updateDefinition.Set(nameof(game.IsPublic), game.IsPublic));
            new MongoClient(mongoUrl).GetDatabase(mongoUrl.DatabaseName).GetCollection<Game>(typeof(Game).Name).UpdateOne(filter, update);
        }
    }
}
