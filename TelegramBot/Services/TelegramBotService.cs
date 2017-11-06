using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using DataModels.Enums;
using DataModels.Extensions;
using DataModels.Interfaces;
using DataModels.Models;
using MongoDB.Bson;
using Newtonsoft.Json;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Exceptions;
using TelegramBot.Extensions;
using TelegramBot.Models;
using Game = DataModels.Models.Game;
using TimeZone = TelegramBot.Resources.TimeZone;
using static TelegramBot.BotCommands;

namespace TelegramBot.Services
{
    /// <summary>
    /// Служба телеграм бота.
    /// </summary>
    public sealed class TelegramBotService : ITelegramBotService
    {
        /// <summary>
        /// Формат даты.
        /// </summary>
        private const string DateTimeFormat = @"H:mm d.MM.yyyy";

        private static readonly string GameInfo = "Название: {0}\nВид спорта: {1}\nДоступность: {2}\nИгроков в команде: {3}\nВремя начала: {4}";

        /// <summary>
        /// Журнал событий.
        /// </summary>
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Менеджер ресурсов.
        /// </summary>
        private static readonly ResourceManager ResourceManager;

        /// <summary>
        /// Словарь видов спорта.
        /// </summary>
        private static readonly Dictionary<string, KindOfSport> KindOfSports;

        /// <summary>
        /// Словарь часовых поясов.
        /// </summary>
        private static readonly Dictionary<TimeZone, string> TimeZone;

        /// <summary>
        /// Хранилище.
        /// </summary>
        private readonly IRepository _repository;

        static TelegramBotService()
        {
            ResourceManager = new ResourceManager("TelegramBot.Resources.MessageStrings", typeof(TelegramBotService).Assembly);
            KindOfSports =
                Enum.GetValues(typeof (KindOfSport))
                    .Cast<KindOfSport>()
                    .ToDictionary(selector => selector.GetElementDescription().ToUpper(), selector => selector);
            TimeZone =
                Enum.GetValues(typeof (TimeZone))
                    .Cast<TimeZone>()
                    .ToDictionary(selector => selector, selector => selector.GetElementDescription());
        }

        /// <summary>
        /// Сессии игроков занятых настройкой игр.
        /// </summary>
        private readonly Dictionary<int, ObjectId> _userGameConfigureSession;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TelegramBotService"/>.
        /// </summary>
        /// <param name="token">Токен бота.</param>
        /// <param name="repository">Хранилище.</param>
        public TelegramBotService(string token, IRepository repository)
        {
            this.BotClient = new TelegramBotClient(token);
            this._repository = repository;
            this._userGameConfigureSession = new Dictionary<int, ObjectId>();
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
            if (e.Message?.Text == null)
            {
                return;
            }

            { 
                var player = this._repository.Get(new Player(e.Message.From.Id)).FirstOrDefault();

                if (e.Message.Text.StartsWith(StartCommand, StringComparison.OrdinalIgnoreCase))
                {
                    if (e.Message.Chat.Type == ChatType.Private)
                    {
                        if (player == null)
                        {
                            Task.Run(() => this.SendSignIn(e.Message));
                            return;
                        }

                        this.BotClient.SendTextMessageAsync(e.Message.Chat, $"{e.Message.From.Username}, Вы уже зарегистрированы");
                        return;
                    }

                    this.BotClient.SendTextMessageAsync(e.Message.Chat, $"Перед началом пользования ботом необходимо зарегистрироваться. Для регистрации отправьте сообщение {StartCommand} в личный чат с ботом");
                    return;
                }

                if (player == null)
                {
                    this.BotClient.SendTextMessageAsync(e.Message.Chat, $"{e.Message.From.Username}, Вы не зарегистрированы. Для регистрации отправьте сообщение {StartCommand} в личный чат с ботом");
                    return;
                }
            }

            if (e.Message.Chat.Type == ChatType.Private)
            {
                if (e.Message.Text.StartsWith(MenuCommand, StringComparison.OrdinalIgnoreCase))
                {
                    lock (this._userGameConfigureSession)
                    {
                        this._userGameConfigureSession.Remove(e.Message.From.Id);
                    }
                    Task.Run(() => this.SendMenu(e.Message.Chat));
                    return;
                }

                if (e.Message.Text.StartsWith(FaqCommand, StringComparison.OrdinalIgnoreCase))
                {
                    Task.Run(() => this.SendFaq(e.Message));
                    return;
                }
            
                Task.Run(() => this.ParseGameParams(e.Message.Chat, e.Message.From.Id, e.Message.Text.Replace(NewGameCommand, string.Empty).Trim().Split(new[] { "\n", $"{Environment.NewLine}" }, StringSplitOptions.RemoveEmptyEntries)));
                return;
            }

            if (e.Message.Text.StartsWith("//", StringComparison.OrdinalIgnoreCase))
            {
                //Task.Run(() => this.DeleteGameFromChat(e.Message));
                return;
            }

            Task.Run(() => this.AddGame(e.Message.Chat, e.Message.Text.Replace("/", string.Empty).Trim()));
        }

        #region Main Command Handlers
        /// <summary>
        /// Выполняет регистрацию игрока в системе.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private async void SendSignIn(Message message)
        {
            string replyMessage = "Для начала пользования ботом необходимо зарегистрироваться";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton(ResourceManager.GetString("SignIn"), SignInCommand),
            });
            await this.BotClient.SendTextMessageAsync(message.Chat, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Отправляет команды меню бота.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора (следует указать, если вызов происходит из команды обратного вызова).</param>
        private async void SendMenu(Chat chat, int messageId = 0)
        {
            string replyMessage = "Выберите действие:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton("Создать игру", ChooseKindOfSportCommand),
                new InlineKeyboardCallbackButton("Мои игры", MyGamesCommand)
            });

            if (messageId == 0)
            {
                await this.BotClient.EditMessageTextAsync(chat, messageId, replyMessage, replyMarkup: inlineReplyMarkup);
                return;
            }

            await this.BotClient.SendTextMessageAsync(chat, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Сообщение с параметрами игры.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="userId">Идентификатор создателя.</param>
        /// <param name="parameters">Массив параметров игры.</param>
        private async void ParseGameParams(Chat chat, int userId, string[] parameters)
        {
            ObjectId gameId;
            lock (this._userGameConfigureSession)
            {
                if (!this._userGameConfigureSession.TryGetValue(userId, out gameId))
                {
                    return;
                }
            }
            
            if (parameters.All(string.IsNullOrEmpty))
            {
                await this.BotClient.SendTextMessageAsync(chat, "Некорректные параметры");
                return;
            }

            Game game;
            try
            {
                game = this._repository.Get(new Game(gameId)).First();
                game.Name = parameters[0];
                game.PlayersPerTeam = int.Parse(parameters[1]);
                game.StartTime = DateTime.ParseExact(parameters[2], DateTimeFormat, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                await this.BotClient.SendTextMessageAsync(chat, "Некорректные параметры");
                return;
            }

            lock (this._userGameConfigureSession)
            {
                this._userGameConfigureSession.Remove(userId);
            }
            this._repository.Update(game);

            await this.BotClient.SendTextMessageAsync(chat, "Команда для добавления игры в чат. Чтобы добавить игру в чат, скопируйте сообщение ниже и отправьте в целевой чат.");
            await this.BotClient.SendTextMessageAsync(chat, $"/{game.Id}");
        }

        /// <summary>
        /// Добавляет игру в чат.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="rawGameId">Идентификатор игры.</param>
        private async void AddGame(Chat chat, string rawGameId)
        {
            if (chat.Type == ChatType.Private)
            {
                await this.BotClient.SendTextMessageAsync(chat, "Добавить игру можно только в чат.");
            }

            ObjectId gameId;
            ObjectId.TryParse(rawGameId, out gameId);

            var game = this._repository.Get(new Game(gameId)).FirstOrDefault();
            if (game == null)
            {
                await this.BotClient.SendTextMessageAsync(chat, "Игра не существует");
                return;
            }

            if (!game.IsPublic)
            {
                if (game.ChatId != chat.Id)
                {
                    await this.BotClient.SendTextMessageAsync(chat, "Игра является частной и уже добавлена в другом чате");
                    return;
                }

                game.ChatId = chat.Id;
                this._repository.Update(game);
            }

            this.SendGameParamsMessage(chat, 0, game);
        }

        /// <summary>
        /// Формирует сообщение с руководством по использованию бота.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private void SendFaq(Message message)
        {
            // ToDo
        }
        #endregion

        /// <summary>
        /// Обрабатывает событие получения обратного вызова.
        /// </summary>
        /// <param name="sender">Инициатор события.</param>
        /// <param name="e">Параметры события.</param>
        private void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            string callbackParams;
            var player = this._repository.Get(new Player(e.CallbackQuery.From.Id)).FirstOrDefault();
            if (player == null)
            {
                if (e.CallbackQuery.Data.ExtractCommandParams(SignInCommand, out callbackParams))
                {
                    this._repository.Add(new Player(e.CallbackQuery.From.Id)
                    {
                        Name = e.CallbackQuery.From.FirstName,
                        Surname = e.CallbackQuery.From.LastName,
                        Nickname = e.CallbackQuery.From.Username
                    });
                    string replyMessage = $"{e.CallbackQuery.From.Username}, Вы успешно зарегистрированы. Выберите Ваш часовой пояс:";
                    IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                    {
                        TimeZone.Select(tz => new InlineKeyboardCallbackButton(tz.Value, $"{TimeZoneCommand} {tz.Key}")).ToArray(),
                    });
                    this.BotClient.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, replyMessage, replyMarkup: inlineReplyMarkup);
                    return;
                }

                this.BotClient.SendTextMessageAsync(e.CallbackQuery.Message.Chat, $"Перед началом пользования ботом необходимо зарегистрироваться. Для регистрации отправьте сообщение {StartCommand} в личный чат с ботом");
                return;
            }

            if (e.CallbackQuery.Data.ExtractCommandParams(TimeZoneCommand, out callbackParams))
            {
                Task.Run(() => this.SetTimeZone(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, callbackParams));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(FinishCommand, StringComparison.OrdinalIgnoreCase))
            {
                lock (this._userGameConfigureSession)
                {
                    this._userGameConfigureSession.Remove(player.TelegramId);
                }
                try
                {
                    this.BotClient.DeleteMessageAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                }
                catch (Exception)
                {
                    // No action
                }
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(MenuCommand, StringComparison.OrdinalIgnoreCase))
            {
                lock (this._userGameConfigureSession)
                {
                    this._userGameConfigureSession.Remove(player.TelegramId);
                }
                Task.Run(() => this.SendMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId));
                return;
            }

            GameParams gameParams;
            if (e.CallbackQuery.Data.ExtractCommandParams(ChooseKindOfSportCommand, out gameParams))
            {
                Task.Run(() => this.SendChooseKindOfSportMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, gameParams));
                return;
            }

            if (e.CallbackQuery.Data.ExtractCommandParams(ChooseGamePrivacyCommand, out gameParams))
            {
                Task.Run(() => this.SendChooseGamePrivacyMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, gameParams));
                return;
            }

            if (e.CallbackQuery.Data.ExtractCommandParams(NewGameCommand, out gameParams))
            {
                if (this._userGameConfigureSession.Keys.Contains(player.TelegramId))
                {
                    return;
                }

                lock (this._userGameConfigureSession)
                {
                    this._userGameConfigureSession.Add(player.TelegramId, ObjectId.Empty);
                }
                gameParams.Player = player;
                Task.Run(() => this.SetGame(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, gameParams));
                return;
            }


            if (e.CallbackQuery.Data.ExtractCommandParams(JoinFirstCommand, out gameParams))
            {
                gameParams.Player = player;
                this.JoinGame(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, JoinFirstCommand, gameParams);
                return;
            }

            if (e.CallbackQuery.Data.ExtractCommandParams(JoinSecondCommand, out gameParams))
            {
                gameParams.Player = player;
                this.JoinGame(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, JoinFirstCommand, gameParams);
                return;
            }

            if (e.CallbackQuery.Data.ExtractCommandParams(DeclineCommand, out gameParams))
            {
                gameParams.Player = player;
                this.DeclineGame(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, gameParams);
                return;
            }
            

            if (e.CallbackQuery.Data.ExtractCommandParams(GetGameCodeCommand, out gameParams))
            {
                Task.Run(() => this.GetGameCode(e.CallbackQuery.Message.Chat, gameParams.Id));
                return;
            }

            if (e.CallbackQuery.Data.ExtractCommandParams(MyGamesCommand, out callbackParams) ||
                e.CallbackQuery.Data.ExtractCommandParams(ToFirstGameCommand, out callbackParams))
            {
                Task.Run(() => this.SendGamesViewMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, "1"));
                return;
            }
            
            if (e.CallbackQuery.Data.ExtractCommandParams(PreviousGameCommand, out callbackParams))
            {
                Task.Run(() => this.SendGamesViewMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, callbackParams));
                return;
            }

            if (e.CallbackQuery.Data.ExtractCommandParams(NextGameCommand, out callbackParams))
            {
                Task.Run(() => this.SendGamesViewMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, callbackParams));
                return;
            }

            if (e.CallbackQuery.Data.ExtractCommandParams(ToLastGameCommand, out callbackParams))
            {
                Task.Run(() => this.SendGamesViewMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, callbackParams));
                return;
            }

            if (e.CallbackQuery.Data.ExtractCommandParams(ToFirstGameCommand, out callbackParams))
            {
                Task.Run(() => this.SendGamesViewMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, callbackParams));
                return;
            }

            if (e.CallbackQuery.Data.ExtractCommandParams(FixGameCommand, out callbackParams))
            {
                Task.Run(() => this.FixGame(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, callbackParams));
                return;
            }
            
            if (e.CallbackQuery.Data.ExtractCommandParams(DeleteGameCommand, out callbackParams))
            {
                Task.Run(() => this.DeleteGame(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player.TelegramId, callbackParams));
                return;
            }
        }

        #region Callback Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chat"></param>
        /// <param name="messageId"></param>
        /// <param name="player"></param>
        /// <param name="callbackParams"></param>
        private void SetTimeZone(Chat chat, int messageId, Player player, string callbackParams)
        {
            int timeZone;
            if (!int.TryParse(callbackParams, out timeZone))
            {
                return;
            }

            player.TimeZone = timeZone;
            this._repository.Update(player);
            this.BotClient.EditMessageTextAsync(chat, messageId, $"{player.}, Ваш часовой пояс сохранён");
        }

        /// <summary>
        /// Отправляет меню выбора вида спорта.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора. Если равно 0, то меню будет отправлено новым сообщением.</param>
        /// <param name="gameParams">Параметры игры.</param>
        private async void SendChooseKindOfSportMenu(Chat chat, int messageId, GameParams gameParams = null)
        {
            string replyMessage = "Выберите вид спорта:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(KindOfSports.Select(kind => new InlineKeyboardButton[]
                                                                                   {
                                                                                       new InlineKeyboardCallbackButton(kind.Value.GetElementDescription(),
                                                                                            $"{ChooseGamePrivacyCommand} {new GameParams(gameParams) {KindOfSport = kind.Value}.ToJson()}")
                                                                                   })
                                                                                   .Concat(new[] { new InlineKeyboardButton[] { new InlineKeyboardCallbackButton("Отмена", MenuCommand)}})
                                                                                   .ToArray()
                                                                      );
            if (messageId == 0)
            {
                await this.BotClient.SendTextMessageAsync(chat, replyMessage, replyMarkup: inlineReplyMarkup);
                return;
            }

            await this.BotClient.EditMessageTextAsync(chat, messageId, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Отправляет меню выбора приватности игры.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="gameParams">Параметры игры.</param>
        private async void SendChooseGamePrivacyMenu(Chat chat, int messageId, GameParams gameParams)
        {
            if (gameParams == null)
            {
                throw new CommandProcessingException("Отсутствуют данные о виде спорта");
            }

            string replyMessage = "Выберите уровень приватности игры:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Частная", $"{NewGameCommand} {new GameParams(gameParams){IsPublic = false}.ToJson()}"),
                    new InlineKeyboardCallbackButton("Общедоступная", $"{NewGameCommand} {new GameParams(gameParams){IsPublic = true}.ToJson()}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Отмена", MenuCommand)
                }
            });
            await this.BotClient.EditMessageTextAsync(chat, messageId, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Задает параметры Игре и сохраняет её в MongoDB.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="gameParams">Параметры игры.</param>
        private async void SetGame(Chat chat, int messageId, GameParams gameParams)
        {
            ObjectId gameId;
            if (gameParams.Id.IsDefault())
            {
                var game = new Game(gameParams.Player)
                {
                    KindOfSport = gameParams.KindOfSport,
                    IsPublic = gameParams.IsPublic
                };

                gameId = this._repository.Add(game);
            }
            else
            {
                var game = this._repository.Get(new Game(gameParams.Id)).FirstOrDefault();
                if (game == null)
                {
                    throw new CommandProcessingException("Игра не существует");
                }
                this._repository.Update(game.ApplyGameParams(gameParams));
                gameId = game.Id;
            }

            lock (this._userGameConfigureSession)
            {
                this._userGameConfigureSession[gameParams.Player.TelegramId] = gameId;
            }
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton("Завершить создание", FinishCommand)
            });
            string text = !gameParams.Id.IsDefault()
                ? "Введите название, максимальное количество игроков в команде и дату начала игры в соответствии с приведённым ниже примером:\nНазвание игры\n5\n20:00 06.10.2017"
                : "Игра создана. Введите название, максимальное количество игроков в команде и дату начала игры в соответствии с приведённым ниже примером:\nНазвание игры\n5\n20:00 06.10.2017";
            await this.BotClient.EditMessageTextAsync(chat, messageId, replyMarkup: inlineReplyMarkup, text: text);
        }

        /// <summary>
        /// Производит присоедиенение к игре.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="joinTo">Указывает к кому присоединиться.</param>
        /// <param name="gameParams">Параметры игры.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private void JoinGame(Chat chat, int messageId, string joinTo, GameParams gameParams)
        {
            if (gameParams == null || gameParams.Id.IsDefault())
            {
                throw new CommandProcessingException("Некорректные данные");
            }
            
            var game = this._repository.Get(new Game(gameParams.Id)).FirstOrDefault();
            if (game == null)
            {
                throw new CommandProcessingException("Игра не существует");
            }

            if (game.FirstTeam == null)
            {
                game.FirstTeam = new Team(string.Empty);
            }
            if (game.SecondTeam == null)
            {
                game.SecondTeam = new Team(string.Empty);
            }
            
            if (joinTo.StartsWith(JoinFirstCommand))
            {
                game.SecondTeam.Players.Remove(gameParams.Player);
                if (game.FirstTeam.Players.Add(gameParams.Player))
                {
                    this._repository.Update(game);
                }
            }
            else
            {
                game.FirstTeam.Players.Remove(gameParams.Player);
                if (game.SecondTeam.Players.Add(gameParams.Player))
                {
                    this._repository.Update(game);
                }
            }
            
            this.SendGameParamsMessage(chat, messageId, game);
        }

        /// <summary>
        /// Производит отказ игрока от игры.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="gameParams">Параметры игры.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private void DeclineGame(Chat chat, int messageId, GameParams gameParams)
        {
            if (gameParams == null || gameParams.Id.IsDefault())
            {
                throw new CommandProcessingException("Некорректные данные");
            }
           
            var game = this._repository.Get(new Game(gameParams.Id)).FirstOrDefault();
            if (game == null)
            {
                throw new CommandProcessingException("Игра не существует");
            }

            if ((game.FirstTeam?.Players?.Remove(gameParams.Player) ?? false) ||
                (game.SecondTeam?.Players?.Remove(gameParams.Player) ?? false))
            {
                this._repository.Update(game);
            }

            this.SendGameParamsMessage(chat, messageId, game);
        }

        /// <summary>
        /// Получает команду для добавления игры в чат.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="objectId">Идентификатор игры.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private async void GetGameCode(Chat chat, ObjectId objectId)
        {
            if (objectId.IsDefault())
            {
                throw new CommandProcessingException("Некорректные данные");
            }
            await this.BotClient.SendTextMessageAsync(chat,
                "Команда для добавления игры в чат. Чтобы добавить игру в чат, скопируйте сообщение ниже и отправьте в целевой чат.");
            await this.BotClient.SendTextMessageAsync(chat, $"/{objectId}");
        }

        /// <summary>
        /// Изменяет параметры игры.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="player">Игрок.</param>
        /// <param name="rawGameId">Идентификатор игры в MongoDB.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private async void FixGame(Chat chat, int messageId, Player player, string rawGameId)
        {
            ObjectId gameId;
            if (string.IsNullOrEmpty(rawGameId))
            {
                throw new CommandProcessingException(CommandProcessingException.BadData);
            }

            if (!ObjectId.TryParse(rawGameId, out gameId))
            {
                throw new CommandProcessingException(CommandProcessingException.BadGameId);
            }

            lock (this._userGameConfigureSession)
            {
                this._userGameConfigureSession[player.TelegramId] = gameId;
            }
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton("Завершить редактирование", FinishCommand)
            });
            await this.BotClient.EditMessageTextAsync(chat, messageId, replyMarkup: inlineReplyMarkup, text: "Редактирование игры");
            this.SendChooseKindOfSportMenu(chat, 0, new GameParams {Id = gameId});
        }

        /// <summary>
        /// Добавляет или обновляет сообщение с параметрами игры в чате.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора (если равно нулю, то будет сгенерировано новое сообщение, иначе заменено указанное).</param>
        /// <param name="game">Игра.</param>
        private void SendGameParamsMessage(Chat chat, int messageId, Game game)
        {
            string firstTeamName = string.IsNullOrEmpty(game.FirstTeam?.Name) ? "А" : game.FirstTeam.Name;
            string secondTeamName = string.IsNullOrEmpty(game.SecondTeam?.Name) ? "Б" : game.SecondTeam.Name;
            Func<Player, string> userNameSelector = player => !string.IsNullOrEmpty(player.Nickname) ? $"@{player.Nickname}" : $"{player.Name} {player.Surname}".Trim();
            string privacy = game.IsPublic ? "Общедоступная" : "Частная";
            string addGameMessage = string.Format(GameInfo, game.Name, game.KindOfSport.GetElementDescription(), privacy, game.PlayersPerTeam, game.StartTime.AddHours(game.Creator.TimeZone).ToString(DateTimeFormat)) + "\n" +
                                    $"{(game.FirstTeam != null && game.FirstTeam.Players.Any() ? $"Команда {firstTeamName}:\n{string.Join(", ", game.FirstTeam.Players.Select(userNameSelector))}\n" : $"Команда {firstTeamName}:\n")}" +
                                    $"{(game.SecondTeam != null && game.SecondTeam.Players.Any() ? $"Команда {secondTeamName}:\n{string.Join(", ", game.SecondTeam.Players.Select(userNameSelector))}" : $"Команда {secondTeamName}:\n")}";
            var teamsKeyboardCallbackButtons = new List<InlineKeyboardButton>();
            string gameParams = new GameParams {Id = game.Id}.ToJson();
            if (game.FirstTeam == null || game.FirstTeam?.Players?.Count != game.PlayersPerTeam)
            {
                teamsKeyboardCallbackButtons.Add(new InlineKeyboardCallbackButton($"За {firstTeamName}", $"{JoinFirstCommand} {gameParams}"));
            }
            if (game.SecondTeam == null || game.SecondTeam?.Players?.Count != game.PlayersPerTeam)
            {
                teamsKeyboardCallbackButtons.Add(new InlineKeyboardCallbackButton($"За {secondTeamName}", $"{JoinSecondCommand} {gameParams}"));
            }

            IReplyMarkup inlineMarkup = new InlineKeyboardMarkup(new[]
            {
                teamsKeyboardCallbackButtons.ToArray(), 
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Отказаться", $"{DeclineCommand} {gameParams}")
                }, 
            });

            if (messageId == 0)
            {
                this.BotClient.SendTextMessageAsync(chat, addGameMessage, replyMarkup: inlineMarkup);
                return;
            }

            this.BotClient.EditMessageTextAsync(chat, messageId, addGameMessage, replyMarkup: inlineMarkup);
        }

        /// <summary>
        /// Удаляет игру.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="playerTelegramId">Игрок.</param>
        /// <param name="rawGameId">Идентификатор игры.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private async void DeleteGame(Chat chat, int messageId, int playerTelegramId, string rawGameId)
        {
            ObjectId gameId;
            if (string.IsNullOrEmpty(rawGameId))
            {
                throw new CommandProcessingException(CommandProcessingException.BadData);
            }

            if (!ObjectId.TryParse(rawGameId, out gameId))
            {
                throw new CommandProcessingException(CommandProcessingException.BadGameId);
            }

            var game = this._repository.Get(new Game(gameId)).FirstOrDefault();
            if (game == null)
            {
                throw new CommandProcessingException(CommandProcessingException.GameNotExist);
            }
            if (!game.Creator.TelegramId.Equals(playerTelegramId))
            {
                throw new CommandProcessingException(CommandProcessingException.NotCreatorTryDelete);
            }

            this._repository.Delete<Game>(game.Id);
            await this.BotClient.EditMessageTextAsync(chat, messageId, "Игра удалена");
        }

        /// <summary>
        /// Отправляет меню обзора доступных игр.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="player">Игрок.</param>
        /// <param name="number">Текущий порядковый номер игры.</param>
        private async void SendGamesViewMenu(Chat chat, int messageId, Player player, string number)
        {
            int gameNumber = 0;
            if (!string.IsNullOrEmpty(number) && !int.TryParse(number.Trim(), out gameNumber))
            {
                throw new CommandProcessingException(CommandProcessingException.BadData);
            }

            var game = gameNumber == 0
                ? this._repository.Get(new Game(player)).LastOrDefault()
                : this._repository.Get(new Game(player), gameNumber, 1).FirstOrDefault();
            if (game == null)
            {
                await this.BotClient.EditMessageTextAsync(chat, messageId, "У Вас нет незавершённых игр");
                return;
            }

            var inlineNavigationKeyboard = new List<InlineKeyboardButton>();
            switch (gameNumber)
            {
                case 0:
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<<", ToFirstGameCommand));
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<", $"{PreviousGameCommand} {gameNumber - 1}"));
                    break;
                case 1:
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">", $"{NextGameCommand} {gameNumber + 1}"));
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">>", ToLastGameCommand));
                    break;
                default:
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<<", ToFirstGameCommand));
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<", $"{PreviousGameCommand} {gameNumber - 1}"));
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">", $"{NextGameCommand} {gameNumber + 1}"));
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">>", ToLastGameCommand));
                    break;
            }

            var gameInfoMessage = $"{gameNumber}.{Environment.NewLine}{string.Format(GameInfo, game.Name, game.KindOfSport.GetElementDescription(), game.StartTime.ToString(DateTimeFormat), game.Id)}";
            IReplyMarkup inlineMarkup = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Получить код", $"{GetGameCodeCommand} {new GameParams{Id = game.Id}.ToJson()}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Редактировать", $"{FixGameCommand} {new GameParams{Id = game.Id}.ToJson()}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Удалить", $"{DeleteGameCommand} {new GameParams{Id = game.Id}.ToJson()}")
                },
                inlineNavigationKeyboard.ToArray()
            });
            await this.BotClient.EditMessageTextAsync(chat, messageId, gameInfoMessage, replyMarkup: inlineMarkup);
        }
        #endregion
    }
}
