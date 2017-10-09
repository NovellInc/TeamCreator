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
using NLog;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using Game = DataModels.Models.Game;

namespace TelegramBot.Services
{
    /// <summary>
    /// Служба телеграм бота.
    /// </summary>
    public sealed class TelegramBotService : ITelegramBotService
    {
        #region Global commands
        private const string StartCommand = "/start";
        private const string MenuCommand = "/menu";
        private const string FaqCommand = "/faq";
        private const string FinishCommand = "/finish";
        private const string BackCommand = "/back";
        private const string SignInCommand = "/signin";
        private const string ChooseKindOfSportCommand = "/choosesport";
        private const string ChooseGamePrivacyCommand = "/choosegameprivacy";
        private const string NewGameCommand = "/newgame";
        private const string SetGameTimeCommand = "/settime";
        private const string GetGameCode = "/gamecode";
        private const string ToFirstGameCommand = "/tofirst";
        private const string PreviousGameCommand = "/previous";
        private const string NextGameCommand = "/next";
        private const string ToLastGameCommand = "/tolast";
        private const string FixGameCommand = "/fixgame";
        private const string DeleteGameCommand = "/deletegame";
        private const string AddGameCommand = "/addgame";
        private const string MyGamesCommand = "/mygames";
        private const string JoinFirst = "joinfirst";
        private const string JoinSecond = "joinsecond";
        private const string Decline = "decline";
        #endregion

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
        }

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
                    Task.Run(() => this.SignIn(player, e.Message));
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
                    Task.Run(() => this.Menu(e.Message));
                    return;
                }

                if (e.Message.Text.StartsWith(FaqCommand, StringComparison.OrdinalIgnoreCase))
                {
                    Task.Run(() => this.Faq(e.Message));
                    return;
                }
            
                Task.Run(() => this.ParseGameParams(e.Message));
                return;
            }

            Task.Run(() => this.AddGame(e.Message));
        }

        #region Main Command Handlers
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
                    string replyMessage = "Для начала пользования ботом необходимо зарегистрироваться";
                    IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        new InlineKeyboardCallbackButton(ResourceManager.GetString("SignIn"), SignInCommand), 
                    });
                    await this.BotClient.SendTextMessageAsync(message.Chat, replyMessage, replyMarkup: inlineReplyMarkup);
                    return;
                }

                await this.BotClient.SendTextMessageAsync(message.Chat, $"{message.From.Username}, Вы уже зарегистрированы");
                return;
            }

            await this.BotClient.SendTextMessageAsync(message.Chat, $"Перед началом пользования ботом необходимо зарегистрироваться. Для регистрации отправьте сообщение {StartCommand} в личный чат с ботом");
        }

        /// <summary>
        /// Отправляет команды меню бота.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        /// <param name="fromCallback">Флаг указывает, что обращение к меню происходит из функции обратного вызова.</param>
        private async void Menu(Message message, bool fromCallback = false)
        {
            string replyMessage = "Выберите действие:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton("Создать игру", ChooseKindOfSportCommand),
                new InlineKeyboardCallbackButton("Мои игры", MyGamesCommand)
            });

            if (fromCallback)
            {
                await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, replyMessage, replyMarkup: inlineReplyMarkup);
                return;
            }

            await this.BotClient.SendTextMessageAsync(message.Chat, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Сообщение на параметры игры.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private async void ParseGameParams(Message message)
        {
            ObjectId gameId;
            lock (this._userGameConfigureSession)
            {
                if (!this._userGameConfigureSession.TryGetValue(message.From.Id, out gameId))
                {
                    return;
                }
            }

            var parameters = message.Text.Replace(NewGameCommand, string.Empty).Trim().Split(new[] { "\n", $"{Environment.NewLine}" }, StringSplitOptions.RemoveEmptyEntries);
            if (parameters.All(string.IsNullOrEmpty))
            {
                await this.BotClient.SendTextMessageAsync(message.Chat, "Некорректные параметры");
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
                await this.BotClient.SendTextMessageAsync(message.Chat, "Некорректные параметры");
                return;
            }

            lock (this._userGameConfigureSession)
            {
                this._userGameConfigureSession.Remove(message.From.Id);
            }
            this._repository.Update(game);

            await this.BotClient.SendTextMessageAsync(message.Chat, "Команда для добавления игры в чат. Чтобы добавить игру в чат, скопируйте сообщение ниже и отправьте в целевой чат.");
            await this.BotClient.SendTextMessageAsync(message.Chat, $"/{game.Id}");
        }
        
        /// <summary>
        /// Добавляет игру в чат.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private async void AddGame(Message message)
        {
            ObjectId gameId;
            ObjectId.TryParse(message.Text.Replace("/", string.Empty).Trim(), out gameId);

            var game = this._repository.Get(new Game(gameId)).FirstOrDefault();
            if (game == null)
            {
                await this.BotClient.SendTextMessageAsync(message.Chat, "Игра не существует");
                return;
            }

            this.SendGameParamsMessage(game, message, true);
        }

        /// <summary>
        /// Формирует сообщение с руководством по использованию бота.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private void Faq(Message message)
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
            var player = this._repository.Get(new Player(e.CallbackQuery.From.Id)).FirstOrDefault();
            if (player == null)
            {
                if (e.CallbackQuery.Data.StartsWith(SignInCommand, StringComparison.OrdinalIgnoreCase))
                {
                    this._repository.Add(new Player(e.CallbackQuery.From.Id)
                    {
                        Name = e.CallbackQuery.From.FirstName,
                        Surname = e.CallbackQuery.From.LastName,
                        Nickname = e.CallbackQuery.From.Username
                    });
                    this.BotClient.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, $"{e.CallbackQuery.From.Username}, Вы успешно зарегистрированы");
                    return;
                }

                this.BotClient.SendTextMessageAsync(e.CallbackQuery.Message.Chat, $"Перед началом пользования ботом необходимо зарегистрироваться. Для регистрации отправьте сообщение {StartCommand} в личный чат с ботом");
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
                Task.Run(() => this.Menu(e.CallbackQuery.Message, true));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(ChooseKindOfSportCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.ChooseKindOfSport(e.CallbackQuery.Message));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(ChooseGamePrivacyCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.ChooseGamePrivacy(e.CallbackQuery.Message, e.CallbackQuery.Data.Replace(ChooseGamePrivacyCommand, string.Empty)));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(NewGameCommand, StringComparison.OrdinalIgnoreCase))
            {
                if (this._userGameConfigureSession.Keys.Contains(player.TelegramId))
                {
                    return;
                }

                lock (this._userGameConfigureSession)
                {
                    this._userGameConfigureSession.Add(player.TelegramId, ObjectId.Empty);
                }
                var gamePrimaryParameters = e.CallbackQuery.Data.Replace(NewGameCommand, string.Empty).Split('|');
                var kindOfSport = KindOfSports[gamePrimaryParameters[0].Trim()];
                Task.Run(() => this.CreateGame(player, e.CallbackQuery.Message, kindOfSport, bool.Parse(gamePrimaryParameters[1])));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(JoinFirst, StringComparison.OrdinalIgnoreCase))
            {
                this.JoinGame(player, e.CallbackQuery.Message, e.CallbackQuery.Data);
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(JoinSecond, StringComparison.OrdinalIgnoreCase))
            {
                this.JoinGame(player, e.CallbackQuery.Message, e.CallbackQuery.Data);
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(Decline, StringComparison.OrdinalIgnoreCase))
            {
                this.DeclineGame(player, e.CallbackQuery.Message, e.CallbackQuery.Data);
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(GetGameCode, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(async () =>
                {
                    await this.BotClient.SendTextMessageAsync(e.CallbackQuery.Message.Chat,
                        "Команда для добавления игры в чат. Чтобы добавить игру в чат, скопируйте сообщение ниже и отправьте в целевой чат.");
                    await this.BotClient.SendTextMessageAsync(e.CallbackQuery.Message.Chat, $"/{e.CallbackQuery.Data}");
                });
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(MyGamesCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.SendGamesViewMenu(e.CallbackQuery.Message, player, 1));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(ToFirstGameCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.SendGamesViewMenu(e.CallbackQuery.Message, player, 1));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(PreviousGameCommand, StringComparison.OrdinalIgnoreCase))
            {
                var gameNumber = int.Parse(e.CallbackQuery.Data.Replace(PreviousGameCommand, string.Empty).Trim());
                Task.Run(() => this.SendGamesViewMenu(e.CallbackQuery.Message, player, gameNumber));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(NextGameCommand, StringComparison.OrdinalIgnoreCase))
            {
                var gameNumber = int.Parse(e.CallbackQuery.Data.Replace(NextGameCommand, string.Empty).Trim());
                Task.Run(() => this.SendGamesViewMenu(e.CallbackQuery.Message, player, gameNumber));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(ToLastGameCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.SendGamesViewMenu(e.CallbackQuery.Message, player, 0));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(FixGameCommand, StringComparison.OrdinalIgnoreCase))
            {
                //Task.Run(() => this.FixGame(player, e.CallbackQuery.Message));
                return;
            }
            
            if (e.CallbackQuery.Data.StartsWith(DeleteGameCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.DeleteGame(player, e.CallbackQuery.Message));
                return;
            }
        }
        
        #region Callback Handlers
        /// <summary>
        /// Отправляет меню выбора вида спорта.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private async void ChooseKindOfSport(Message message)
        {
            string replyMessage = "Выберите вид спорта:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(KindOfSports.Select(kind => new InlineKeyboardButton[]
                                                                                   {
                                                                                       new InlineKeyboardCallbackButton(kind.Value.GetElementDescription(), $"{ChooseGamePrivacyCommand} {kind.Key}")
                                                                                   })
                                                                                   .Concat(new[] { new InlineKeyboardButton[] { new InlineKeyboardCallbackButton("Отмена", MenuCommand)}})
                                                                                   .ToArray()
                                                                      );
            await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Отправляет меню выбора приватности игры.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        /// <param name="kindOfSport">Вид спорта.</param>
        private async void ChooseGamePrivacy(Message message, string kindOfSport)
        {
            string replyMessage = "Выберите уровень приватности игры:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Частная", $"{NewGameCommand} {kindOfSport}|{false}"),
                    new InlineKeyboardCallbackButton("Общедоступная", $"{NewGameCommand} {kindOfSport}|{true}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Отмена", MenuCommand)
                }
            });
            await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Создаёт новую игру.
        /// </summary>
        /// <param name="player">Создатель игры.</param>
        /// <param name="message">Сообщение.</param>
        /// <param name="kindOfSport">Вид спорта.</param>
        /// <param name="isPublic">Признак общедоступности игры.</param>
        private async void CreateGame(Player player, Message message, KindOfSport kindOfSport, bool isPublic)
        {
            var game = new Game(player)
            {
                KindOfSport = kindOfSport,
                IsPublic = isPublic
            };

            var gameId = this._repository.Add(game);
            lock (this._userGameConfigureSession)
            {
                this._userGameConfigureSession[player.TelegramId] = gameId;
            }
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton("Завершить создание", FinishCommand)
            });
            await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, replyMarkup: inlineReplyMarkup, text: "Игра создана. Введите название, максимальное количество игроков в команде и дату начала игры в соответствии с приведённым ниже примером:\nНазвание игры\n5\n20:00 06.10.2017");
        }

        /// <summary>
        /// Производит присоедиенение к игре.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="message">Сообщение.</param>
        /// <param name="joinTo">Данные для присоединения.</param>
        private async void JoinGame(Player player, Message message, string joinTo)
        {
            ObjectId gameId;
            if (joinTo.StartsWith(JoinFirst))
            {
                ObjectId.TryParse(joinTo.Replace(JoinFirst, string.Empty).Trim(), out gameId);
                joinTo = JoinFirst;
            }
            else
            {
                ObjectId.TryParse(joinTo.Replace(JoinSecond, string.Empty).Trim(), out gameId);
                joinTo = JoinSecond;
            }

            var game = this._repository.Get(new Game(gameId)).FirstOrDefault();
            if (game == null)
            {
                await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, "Игра не существует");
                return;
            }

            if (game.FirstTeam == null)
            {
                game.FirstTeam = new Team(string.Empty);
            }
            if (game.SecondTeam == null)
            {
                game.SecondTeam = new Team(string.Empty);
            }
            
            if (joinTo.Equals(JoinFirst))
            {
                game.SecondTeam.Players.Remove(player);
                if (game.FirstTeam.Players.Add(player))
                {
                    this._repository.Update(game);
                }
            }
            else
            {
                game.FirstTeam.Players.Remove(player);
                if (game.SecondTeam.Players.Add(player))
                {
                    this._repository.Update(game);
                }
            }
            
            this.SendGameParamsMessage(game, message, false);
        }

        /// <summary>
        /// Производит отказ игрока от игры.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="message">Сообщение.</param>
        /// <param name="data">Данные для отказа от игры.</param>
        private async void DeclineGame(Player player, Message message, string data)
        {
            ObjectId gameId;
            ObjectId.TryParse(data.Replace(Decline, string.Empty).Trim(), out gameId);

            var game = this._repository.Get(new Game(gameId)).FirstOrDefault();
            if (game == null)
            {
                await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, "Игра не существует");
                return;
            }

            if ((game.FirstTeam?.Players?.Remove(player) ?? false) ||
                (game.SecondTeam?.Players?.Remove(player) ?? false))
            {
                this._repository.Update(game);
            }

            this.SendGameParamsMessage(game, message, false);
        }

        /// <summary>
        /// Изменяет параметры игры.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="message">Сообщение.</param>
        private void FixGame(Player player, Message message)
        {
        }

        /// <summary>
        /// Добавляет или обновляет сообщение с параметрами игры в чате.
        /// </summary>
        /// <param name="game">Игра.</param>
        /// <param name="message">Сообщение.</param>
        /// <param name="createNew">Указывает, что необходимо создать новое сообщение.</param>
        private void SendGameParamsMessage(Game game, Message message, bool createNew)
        {
            string firstTeamName = string.IsNullOrEmpty(game.FirstTeam?.Name) ? "А" : game.FirstTeam.Name;
            string secondTeamName = string.IsNullOrEmpty(game.SecondTeam?.Name) ? "Б" : game.SecondTeam.Name;
            string privacy = game.IsPublic ? "Общедоступная" : "Частная";
            string addGameMessage = string.Format(GameInfo, game.Name, game.KindOfSport.GetElementDescription(), privacy, game.PlayersPerTeam, game.StartTime.ToString(DateTimeFormat)) + "\n" +
                                    $"{(game.FirstTeam != null && game.FirstTeam.Players.Any() ? $"Команда {firstTeamName}:\n{string.Join(", ", game.FirstTeam.Players.Select(player => $"@{player.Nickname}"))}\n" : $"Команда {firstTeamName}:\n")}" +
                                    $"{(game.SecondTeam != null && game.SecondTeam.Players.Any() ? $"Команда {secondTeamName}:\n{string.Join(", ", game.SecondTeam.Players.Select(player => $"@{player.Nickname}"))}" : $"Команда {secondTeamName}:\n")}";
            var teamsKeyboardCallbackButtons = new List<InlineKeyboardButton>();
            if (game.FirstTeam == null || game.FirstTeam?.Players?.Count != game.PlayersPerTeam)
            {
                teamsKeyboardCallbackButtons.Add(new InlineKeyboardCallbackButton($"За {firstTeamName}", $"{JoinFirst} {game.Id}"));
            }
            if (game.SecondTeam == null || game.SecondTeam?.Players?.Count != game.PlayersPerTeam)
            {
                teamsKeyboardCallbackButtons.Add(new InlineKeyboardCallbackButton($"За {secondTeamName}", $"{JoinSecond} {game.Id}"));
            }

            IReplyMarkup inlineMarkup = new InlineKeyboardMarkup(new[]
            {
                teamsKeyboardCallbackButtons.ToArray(), 
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Отказаться", $"{Decline} {game.Id}")
                }, 
            });

            if (createNew)
            {
                this.BotClient.SendTextMessageAsync(message.Chat, addGameMessage, replyMarkup: inlineMarkup);
                return;
            }

            this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, addGameMessage, replyMarkup: inlineMarkup);
        }

        /// <summary>
        /// Удаляет игру.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="message">Сообщение.</param>
        private async void DeleteGame(Player player, Message message)
        {
            ObjectId gameId;
            ObjectId.TryParse(message.Text.Replace(DeleteGameCommand, string.Empty).Trim(), out gameId);
            if (gameId.IsDefault())
            {
                await this.BotClient.SendTextMessageAsync(message.Chat, "Некорректный идентификатор");
                return;
            }

            var game = this._repository.Get(new Game(gameId)).FirstOrDefault();
            if (game == null)
            {
                await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, "Игра не существует");
                return;
            }
            if (!game.Creator.Equals(player))
            {
                await this.BotClient.SendTextMessageAsync(message.Chat, "Только создатель игры может удалить игру");
                return;
            }

            this._repository.Delete<Game>(game.Id);
            await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, "Игра удалена");
        }
        
        /// <summary>
        /// Отправляет меню обзора доступных игр.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        /// <param name="player">Игрок.</param>
        /// <param name="number">Текущий порядковый номер игры.</param>
        private async void SendGamesViewMenu(Message message, Player player, int number)
        {
            var game = number == 0
                ? this._repository.Get(new Game(player)).LastOrDefault()
                : this._repository.Get(new Game(player), number, 1).FirstOrDefault();
            if (game == null)
            {
                await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, "У Вас нет незавершённых игр");
                return;
            }

            var inlineNavigationKeyboard = new List<InlineKeyboardButton>();
            switch (number)
            {
                case 0:
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<<", ToFirstGameCommand));
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<", $"{PreviousGameCommand} {number - 1}"));
                    break;
                case 1:
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">", $"{NextGameCommand} {number + 1}"));
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">>", ToLastGameCommand));
                    break;
                default:
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<<", ToFirstGameCommand));
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<", $"{PreviousGameCommand} {number - 1}"));
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">", $"{NextGameCommand} {number + 1}"));
                    inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">>", ToLastGameCommand));
                    break;
            }

            var gameInfoMessage = $"{number}.{Environment.NewLine}{string.Format(GameInfo, game.Name, game.KindOfSport.GetElementDescription(), game.StartTime.ToString(DateTimeFormat), game.Id)}";
            IReplyMarkup inlineMarkup = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Получить код", $"{GetGameCode} {game.Id}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Редактировать", $"{FixGameCommand} {game.Id}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Удалить", $"{DeleteGameCommand} {game.Id}")
                },
                inlineNavigationKeyboard.ToArray()
            });
            await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, gameInfoMessage, replyMarkup: inlineMarkup);
        }
        #endregion
    }
}
