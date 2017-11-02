using DataModels.Enums;
using DataModels.Extensions;
using DataModels.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Newtonsoft.Json;
using TelegramBot;
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
            string json = new GameParams
            {
                KindOfSport = KindOfSport.Football,
                IsPublic = true
            }.ToJson();
            string data = "string";
            string commandParams = $"{BotCommands.AddGameCommand} {data.ToJson()}";
            GameParams gameParams;
            commandParams.ExtractCommandParams(BotCommands.AddGameCommand, out gameParams);
        }
    }
}
