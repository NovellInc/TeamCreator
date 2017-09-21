using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DataModels.Enums;
using DataModels.Extensions;
using DataModels.Interfaces;
using DataModels.Models;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Game = DataModels.Models.Game;

namespace TelegramBot.Services
{
    /// <summary>
    /// Служба телеграм бота.
    /// </summary>
    public sealed class TelegramBotService : ITelegramBotService
    {
        #region Global commands
        private const string SignInCommand = "/signin";
        private const string NewGameCommand = "/newgame";
        private const string SetGameTime = "/settime";
        private const string FixGameCommand = "/fixgame";
        private const string DeleteGameCommand = "/deletegame";
        private const string AddGameToChat = "/addgame";
        private const string MyGames = "/mygame";
        #endregion

        /// <summary>
        /// Журнал событий.
        /// </summary>
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Хранилище.
        /// </summary>
        private readonly IRepository _repository;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TelegramBotService"/>.
        /// </summary>
        /// <param name="token">Токен бота.</param>
        /// <param name="repository">Хранилище.</param>
        public TelegramBotService(string token, IRepository repository)
        {
            this.BotClient = new TelegramBotClient(token);
            this._repository = repository;
        }

        /// <summary>
        /// Запускает бота.
        /// </summary>
        public void Start()
        {
            this.BotClient.OnMessage += this.OnMessage;
            this.BotClient.OnCallbackQuery += this.OnCallbackQuery;
            this.BotClient.StartReceiving();
            Log.Info("Телеграм бот запущен.");
        }

        /// <summary>
        /// Останавливает бота.
        /// </summary>
        public void Stop()
        {
            this.BotClient.OnMessage -= this.OnMessage;
            this.BotClient.OnCallbackQuery -= this.OnCallbackQuery;
            this.BotClient.StopReceiving();
            Log.Info("Телеграм бот остановлен.");
        }

        /// <summary>
        /// Получает клиента телеграм бота.
        /// </summary>
        public TelegramBotClient BotClient { get; }

        /// <summary>
        /// Обрабатывает событие получения сообщения.
        /// </summary>
        /// <param name="sender">Инициатор события.</param>
        /// <param name="e">Параметры события.</param>
        private void OnMessage(object sender, MessageEventArgs e)
        {
            var player = this._repository.Get(new Player(e.Message.From.Id)).FirstOrDefault();

            if (e.Message.Text.StartsWith(SignInCommand))
            {
                Task.Run(() => this.SignIn(player, e.Message));
                return;
            }

            if (player == null)
            {
                this.BotClient.SendTextMessageAsync(e.Message.Chat, $"{e.Message.From.Username}, Вы не зарегистрированы. Для регистрации отправьте сообщение {SignInCommand} в личный чат с ботом");
                return;
            }

            if (e.Message.Text.StartsWith(NewGameCommand))
            {
                Task.Run(() => this.NewGame(player, e.Message));
                return;
            }

            if (e.Message.Text.StartsWith(FixGameCommand))
            {
                Task.Run(() => this.FixGame(player, e.Message));
                return;
            }
        }

        /// <summary>
        /// Обрабатывает событие получения обратного вызова.
        /// </summary>
        /// <param name="sender">Инициатор события.</param>
        /// <param name="e">Параметры события.</param>
        private void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            
        }

        /// <summary>
        /// Выполняет регистрацию игрока в системе.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="message">Сообщение.</param>
        private async void SignIn(Player player, Message message)
        {
            if (message.Chat.Type == ChatType.Private)
            {
                if (player == null)
                {
                    this._repository.Add(new Player(message.From.Id)
                    {
                        Name = message.From.FirstName,
                        Surname = message.From.LastName,
                        Nickname = message.From.Username
                    });
                    await this.BotClient.SendTextMessageAsync(message.Chat, $"{message.From.Username}, Вы успешно зарегистрированы");
                }

                await this.BotClient.SendTextMessageAsync(message.Chat, $"{message.From.Username}, Вы уже зарегистрированы");
                return;
            }

            await this.BotClient.SendTextMessageAsync(message.Chat, $"Для регистрации отправьте сообщение {SignInCommand} в личный чат с ботом");
        }

        /// <summary>
        /// Создаёт новую игру.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="message">Сообщение.</param>
        private async void NewGame(Player player, Message message)
        {
            var parameters = message.Text.Replace(NewGameCommand, string.Empty).Trim().Split(new []{"\n", $"{Environment.NewLine}"}, StringSplitOptions.RemoveEmptyEntries);
            var game = new Game(player);
            if (parameters.All(string.IsNullOrEmpty))
            {
                var gameId = this._repository.Add(game);
                await this.BotClient.SendTextMessageAsync(message.Chat, $"Создана игра. Идентификатор: {gameId}");
                return;
            }

            try
            {
                game.Name = parameters[0];
                game.KindOfSport = Enum.GetValues(typeof(KindOfSport)).Cast<KindOfSport>().First(kindOfSport => kindOfSport.GetClassDescription().Equals(parameters[1], StringComparison.InvariantCultureIgnoreCase));
                game.StartTime = DateTime.ParseExact(parameters[2], "HH:mm dd.MM.yyyy", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                await this.BotClient.SendTextMessageAsync(message.Chat, "Некорректные параметры");
            }
        }

        /// <summary>
        /// Изменяет параметры игры.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="message">Сообщение.</param>
        private void FixGame(Player player, Message message)
        {
        }
    }
}
