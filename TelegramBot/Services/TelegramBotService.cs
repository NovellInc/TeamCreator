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
        private const string NewGameCommand = "/newgame";
        private const string SetGameTimeCommand = "/settime";
        private const string FixGameCommand = "/fixgame";
        private const string DeleteGameCommand = "/deletegame";
        private const string AddGameCommand = "/addgame";
        private const string MyGamesCommand = "/mygames";
        private const string JoinFirst = "joinfirst";
        private const string JoinSecond = "joinsecond";
        private const string Decline = "decline";
        #endregion

        private const string DateTimeFormat = @"HH:mm dd.MM.yyyy";

        private static readonly string GameInfo = "Название: {0}\nВид спорта: {1}\nВремя начала: {2}\nИдентификатор: {3}";

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

            if (e.Message.Text.StartsWith(MenuCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.Menu(e.Message));
                return;
            }

            if (e.Message.Text.StartsWith(FaqCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.Faq(e.Message));
                return;
            }
            
            Task.Run(() => this.ParseGameParams(e.Message));
        }

        /// <summary>
        /// Отправляет команды меню бота.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private async void Menu(Message message)
        {
            string replyMessage = "Выберите действие:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton("Создать игру", ChooseKindOfSportCommand),
                new InlineKeyboardCallbackButton("Мои игры", MyGamesCommand)
            });
            await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Сообщение на параметры игры.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private void ParseGameParams(Message message)
        {
            ObjectId gameId;
            if (!this._userGameConfigureSession.TryGetValue(message.From.Id, out gameId))
            {
                return;
            }

            var parameters = message.Text.Replace(NewGameCommand, string.Empty).Trim().Split(new[] { "\n", $"{Environment.NewLine}" }, StringSplitOptions.RemoveEmptyEntries);
            if (parameters.All(string.IsNullOrEmpty))
            {
                this.BotClient.SendTextMessageAsync(message.Chat, "Некорректные параметры");
                return;
            }

            Game game;
            try
            {
                game = this._repository.Get(new Game(gameId)).First();
                game.KindOfSport = KindOfSports[parameters[0].ToUpper()];
                game.StartTime = DateTime.ParseExact(parameters[1], DateTimeFormat, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                this.BotClient.SendTextMessageAsync(message.Chat, "Некорректные параметры");
                return;
            }

            this._userGameConfigureSession.Remove(message.From.Id);
            this._repository.Update(game);
            this.SendGameParamsMessage(game, message, true);
        }

        /// <summary>
        /// Формирует сообщение с руководством по использованию бота.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private void Faq(Message message)
        {

        }

        /// <summary>
        /// Обрабатывает событие получения обратного вызова.
        /// </summary>
        /// <param name="sender">Инициатор события.</param>
        /// <param name="e">Параметры события.</param>
        private void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
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

            if (e.CallbackQuery.Data.StartsWith(MenuCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.Menu(e.CallbackQuery.Message));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(ChooseKindOfSportCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.ChooseKindOfSport(e.CallbackQuery.Message));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(NewGameCommand, StringComparison.OrdinalIgnoreCase))
            {
                var kindOfSport = KindOfSports[e.CallbackQuery.Data.Replace(NewGameCommand, string.Empty).Trim()];
                Task.Run(() => this.CreateGame(e.CallbackQuery.From.Id, e.CallbackQuery.Message, kindOfSport));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(JoinFirst, StringComparison.OrdinalIgnoreCase))
            {
                this.JoinGame(e.CallbackQuery.From.Id, e.CallbackQuery.Message, JoinFirst);
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(JoinSecond, StringComparison.OrdinalIgnoreCase))
            {
                this.JoinGame(e.CallbackQuery.From.Id, e.CallbackQuery.Message, JoinSecond);
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(FixGameCommand, StringComparison.OrdinalIgnoreCase))
            {
                //Task.Run(() => this.FixGame(player, e.CallbackQuery.Message));
                return;
            }
            
            if (e.CallbackQuery.Data.StartsWith(DeleteGameCommand, StringComparison.OrdinalIgnoreCase))
            {
                //Task.Run(() => this.DeleteGame(player, e.CallbackQuery.Message));
                return;
            }

            if (e.CallbackQuery.Data.StartsWith(MyGamesCommand, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => this.MyGames(e.CallbackQuery.From.Id, e.CallbackQuery.Message));
                return;
            }
        }

        /// <summary>
        /// Производит присоедиенение к игре.
        /// </summary>
        /// <param name="playerId">Идентификатор игрока в Telegram.</param>
        /// <param name="message">Сообщение.</param>
        /// <param name="joinTo">Данные для присоединения.</param>
        private async void JoinGame(int playerId, Message message, string joinTo)
        {
            var player = this._repository.Get(new Player(playerId)).FirstOrDefault();
            if (player == null)
            {
                return;
            }

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

            if (joinTo.Equals(JoinFirst))
            {
                if (game.FirstTeam.Players.Contains(player) || game.SecondTeam.Players.Contains(player))
                {
                    return;
                }

                game.FirstTeam.Players.Add(player);
            }
            else
            {
                if (game.FirstTeam.Players.Contains(player) || game.SecondTeam.Players.Contains(player))
                {
                    return;
                }

                game.SecondTeam.Players.Add(player);
            }

            this._repository.Update(game);
            this.SendGameParamsMessage(game, message, false);
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
                    string replyMessage = "Для начала пользования ботом необходимо зарегистрироваться";
                    IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        new InlineKeyboardCallbackButton(ResourceManager.GetString("SignIn"), SignInCommand), 
                    });
                    await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, replyMessage, replyMarkup: inlineReplyMarkup);
                    return;
                }

                await this.BotClient.SendTextMessageAsync(message.Chat, $"{message.From.Username}, Вы уже зарегистрированы");
                return;
            }

            await this.BotClient.SendTextMessageAsync(message.Chat, $"Перед началом пользования ботом необходимо зарегистрироваться. Для регистрации отправьте сообщение {StartCommand} в личный чат с ботом");
        }

        /// <summary>
        /// Отправляет меню выбора вида спорта.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        private async void ChooseKindOfSport(Message message)
        {
            string replyMessage = "Выберите вид спорта:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(KindOfSports.Select(kind => new InlineKeyboardButton[]
                                                                                   {
                                                                                       new InlineKeyboardCallbackButton(kind.Key, $"{NewGameCommand} {kind.Key}")
                                                                                   })
                                                                                   .Concat(new[] { new InlineKeyboardButton[] { new InlineKeyboardCallbackButton("Назад", MenuCommand)}})
                                                                                   .ToArray()
                                                                      );
            await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Создаёт новую игру.
        /// </summary>
        /// <param name="creatorId">Идентификатор создателя игры.</param>
        /// <param name="message">Сообщение.</param>
        /// <param name="kindOfSport">Вид спорта.</param>
        private async void CreateGame(int creatorId, Message message, KindOfSport kindOfSport)
        {
            Player player;
            try
            {
                player = this._repository.Get(new Player(message.From.Id)).FirstOrDefault();
            }
            catch (Exception)
            {
                return;
            }
            
            var game = new Game(player)
            {
                KindOfSport = kindOfSport
            };

            var gameId = this._repository.Add(game);
            this._userGameConfigureSession.Add(creatorId, gameId);
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton("Завершить создание", FinishCommand)
            });
            await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, "Игра создана. Введите название и дату начала игры в соответствии с приведённым ниже примером:\nРазминка\n20:00 06.10.2017");
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
        private async void SendGameParamsMessage(Game game, Message message, bool createNew)
        {
            if (game == null)
            {
                await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, "Игра не существует");
                return;
            }
            
            string firstTeamName = string.IsNullOrEmpty(game.FirstTeam.Name) ? "А" : game.FirstTeam.Name;
            string secondTeamName = string.IsNullOrEmpty(game.SecondTeam.Name) ? "Б" : game.SecondTeam.Name;
            string addGameMessage = string.Format(GameInfo, game.Name, game.KindOfSport.GetElementDescription(), game.StartTime.ToString(DateTimeFormat)) + "\n" +
                                    $"{(game.FirstTeam.Players.Any() ? $"Команда {firstTeamName}:\n{string.Join(", ", game.FirstTeam.Players.Select(player => $"@{player.Nickname}"))}\n" : string.Empty)}" +
                                    $"{(game.SecondTeam.Players.Any() ? $"Команда {secondTeamName}:\n{string.Join(", ", game.SecondTeam.Players.Select(player => $"@{player.Nickname}"))}" : string.Empty)}";
            IReplyMarkup inlineMarkup = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton($"За {firstTeamName}", $"{JoinFirst} {game.Id}"),
                    new InlineKeyboardCallbackButton($"За {secondTeamName}", $"{JoinSecond} {game.Id}")
                }, 
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Отказаться", Decline)
                }, 
            });

            if (createNew)
            {
                await this.BotClient.SendTextMessageAsync(message.Chat, addGameMessage, replyMarkup: inlineMarkup);
                return;
            }

            await this.BotClient.EditMessageTextAsync(message.Chat, message.MessageId, addGameMessage, replyMarkup: inlineMarkup);
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
                await this.BotClient.SendTextMessageAsync(message.Chat, "Игра не существует");
                return;
            }
            if (!game.Creator.Equals(player))
            {
                await this.BotClient.SendTextMessageAsync(message.Chat, "Только создатель игры может удалить игру");
                return;
            }

            this._repository.Delete<Game>(game.Id);
            await this.BotClient.SendTextMessageAsync(message.Chat, "Игра удалена");
        }

        /// <summary>
        /// Получает информацию о незавершённых играх для игрока.
        /// </summary>
        /// <param name="from">Идентификатор игрока в Telegram.</param>
        /// <param name="message">Сообщение.</param>
        private async void MyGames(int from, Message message)
        {
            if (message.Chat.Type != ChatType.Private)
            {
                await this.BotClient.SendTextMessageAsync(message.Chat, "Просматривать игры можно только в личном чате с ботом");
                return;
            }

            var player = this._repository.Get(new Player(from)).First();
            var games = this._repository.Get(new Game(player));
            if (games == null)
            {
                await this.BotClient.SendTextMessageAsync(message.Chat, "У Вас нет незавершённых игр");
                return;
            }

            if (games.Count > 5)
            {
                //ToDo: менюшку со скроллингом
            }

            await this.BotClient.SendTextMessageAsync(message.Chat, string.Join($"{Environment.NewLine}{Environment.NewLine}", games.Select(game => string.Format(GameInfo, game.Name, game.KindOfSport.GetElementDescription(), game.StartTime.ToString(DateTimeFormat), game.Id))));
        }
    }
}
