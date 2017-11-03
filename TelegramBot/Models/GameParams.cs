using System;
using DataModels.Enums;
using DataModels.Extensions;
using DataModels.Models;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace TelegramBot.Models
{
    /// <summary>
    /// Класс описывает параметры игры.
    /// </summary>
    [JsonObject]
    public sealed class GameParams
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="GameParams"/> с заданными параметрами.
        /// </summary>
        /// <param name="gameParams">Объект для копирования.</param>
        public GameParams(GameParams gameParams)
        {
            if (gameParams == null)
            {
                return;
            }
            
            if (!gameParams.Id.IsDefault())
            {
                Id = gameParams.Id;
            }
            if (!gameParams.Player.IsDefault())
            {
                Player = gameParams.Player;
            }
            if (!gameParams.KindOfSport.IsDefault())
            {
                KindOfSport = gameParams.KindOfSport;
            }
            if (!gameParams.Name.IsDefault())
            {
                Name = gameParams.Name;
            }
            if (!gameParams.IsPublic.IsDefault())
            {
                IsPublic = gameParams.IsPublic;
            }
            if (!gameParams.ChatId.IsDefault())
            {
                ChatId = gameParams.ChatId;
            }
            if (!gameParams.StartTime.IsDefault())
            {
                StartTime = gameParams.StartTime;
            }
            if (!gameParams.PlayersPerTeam.IsDefault())
            {
                PlayersPerTeam = gameParams.PlayersPerTeam;
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="GameParams"/>.
        /// </summary>
        public GameParams()
        {
        }

        /// <summary>
        /// Идентификатор в MongoDB.
        /// </summary>
        public ObjectId Id { get; set; }

        /// <summary>
        /// Получает или задает участника игры.
        /// </summary>
        public Player Player { get; set; }

        /// <summary>
        /// Получает или задает вид спорта игры.
        /// </summary>
        public KindOfSport KindOfSport { get; set; }

        /// <summary>
        /// Получает или задает название игры.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Получает или задает признак общедоступности игры.
        /// </summary>
        public bool IsPublic { get; set; }
        
        /// <summary>
        /// Получает или задает начало игры.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Получает или задает максимальное количество игроков в команде.
        /// </summary>
        public int PlayersPerTeam { get; set; }
    }

    /// <summary>
    /// Класс расширений для Игры.
    /// </summary>
    public static class GameExtension
    {
        /// <summary>
        /// Применяет новые параметры игры к Игре.
        /// </summary>
        /// <param name="game">Игра.</param>
        /// <param name="gameParams">Параметры игры.</param>
        /// <returns>Возвращает Игру.</returns>
        public static Game ApplyGameParams(this Game game, GameParams gameParams)
        {
            game.IsPublic = gameParams.IsPublic;
            if (!gameParams.KindOfSport.IsDefault())
            {
                game.KindOfSport = gameParams.KindOfSport;
            }
            if (!gameParams.Name.IsDefault())
            {
                game.Name = gameParams.Name;
            }
            if (!gameParams.StartTime.IsDefault())
            {
                game.StartTime = gameParams.StartTime;
            }
            if (!gameParams.PlayersPerTeam.IsDefault())
            {
                game.PlayersPerTeam = gameParams.PlayersPerTeam;
            }

            return game;
        }
    }
}
