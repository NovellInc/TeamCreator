using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using TelegramBot.Exceptions;
using TelegramBot.Extensions;
using TelegramBot.Models;
using static TelegramBot.Models.MainBotCommands;
using static TelegramBot.Models.CallbackBotCommands;
using Game = DataModels.Models.Game;

namespace TelegramBot.Services
{
    /// <summary>
    /// Служба телеграм бота.
    /// </summary>
    public sealed class TelegramBotService : ITelegramBotService
    {
        public const string CommonError = "Ошибка выполнения команды";
        public const string BadDataError = "Некорректные данные";
        public const string BadDataTryAgainError = "Некорректные данные. Попробуйте снова";
        public const string BadGameIdError = "Некорректный идентификатор игры";
        public const string GameNotExistError = "Игра не существует";
        public const string NotCreatorTryDeleteError = "Только создатель игры может удалить игру";
        public const string PrivateGameIsAlreadyAdded = "Игра является частной и уже добавлена в другом чате";
        public const string OnlyChatAvailable = "Добавить игру можно только в чат";

        /// <summary>
        /// Разделитель команды и параметров в данных обратного вызова.
        /// </summary>
        private const char CommandAndParamsSplitter = ' ';
        
        /// <summary>
        /// Разделитель параметров в данных обратного вызова.
        /// </summary>
        private const char ParamsSplitter = '|';

        /// <summary>
        /// Формат даты.
        /// </summary>
        private const string DateTimeFormat = @"H:mm d.MM.yyyy";

        /// <summary>
        /// Шаблон информации об игре.
        /// </summary>
        private const string GameInfoPattern = "Название: {0}\nВид спорта: {1}\nДоступность: {2}\nИгроков в команде: {3}\nВремя начала: {4}";

        /// <summary>
        /// Журнал событий.
        /// </summary>
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Менеджер ресурсов.
        /// </summary>
        //private static readonly ResourceManager ResourceManager;

        /// <summary>
        /// Словарь видов спорта.
        /// </summary>
        private static readonly Dictionary<string, KindOfSport> KindOfSports;

        /// <summary>
        /// Словарь часовых поясов.
        /// </summary>
        private static readonly Dictionary<TimeZoneOffset, string> TimeZone;

        /// <summary>
        /// Хранилище.
        /// </summary>
        private readonly IRepository _repository;

        static TelegramBotService()
        {
            //ResourceManager = new ResourceManager("TelegramBot.Resources.MessageStrings", typeof(TelegramBotService).Assembly);
            KindOfSports =
                Enum.GetValues(typeof (KindOfSport))
                    .Cast<KindOfSport>()
                    .Skip(1)
                    .ToDictionary(selector => selector.GetElementDescription().ToUpper(), selector => selector);
            TimeZone =
                Enum.GetValues(typeof (TimeZoneOffset))
                    .Cast<TimeZoneOffset>()
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
            this.BotClient.OnMessage += this.OnMessage;
            this.BotClient.OnCallbackQuery += this.OnCallbackQuery;
            this._repository = repository;
            this._userGameConfigureSession = new Dictionary<int, ObjectId>();
        }

        /// <summary>
        /// Запускает бота.
        /// </summary>
        public void Start()
        {
            this.BotClient.StartReceiving();
            Log.Info("Телеграм бот запущен.");
        }

        /// <summary>
        /// Останавливает бота.
        /// </summary>
        public void Stop()
        {
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
        private async void OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message?.Text == null)
            {
                return;
            }
            
            try
            {
                var player = this._repository.Get(new Player(e.Message.From.Id)).FirstOrDefault();
                if (e.Message.Text.StartsWith(StartCommand, StringComparison.OrdinalIgnoreCase))
                {
                    if (e.Message.Chat.Type != ChatType.Private)
                    {
                        throw new CommandProcessingException($"Перед началом пользования ботом необходимо зарегистрироваться. Для регистрации отправьте сообщение {StartCommand} в личный чат с ботом");
                    }

                    if (player != null)
                    {
                        throw new CommandProcessingException($"{e.Message.From.Username}, Вы уже зарегистрированы");
                    }

                    await this.SendSignIn(e.Message.Chat);
                    return;
                }

                if (player == null)
                {
                    throw new CommandProcessingException($"{e.Message.From.Username}, Вы не зарегистрированы. Для регистрации отправьте сообщение {StartCommand} в личный чат с ботом");
                }


                if (e.Message.Chat.Type == ChatType.Private)
                {
                    if (e.Message.Text.StartsWith(MenuCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        lock (this._userGameConfigureSession)
                        {
                            this._userGameConfigureSession.Remove(e.Message.From.Id);
                        }
                        await this.SendMenu(e.Message.Chat);
                        return;
                    }

                    if (e.Message.Text.StartsWith(FaqCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        this.SendFaq(e.Message.Chat);
                        return;
                    }

                    await this.ParseGameParams(e.Message.Chat,
                        e.Message.From.Id,
                        e.Message.Text.Trim()
                            .Split(new[] {"\n", $"{Environment.NewLine}"}, StringSplitOptions.RemoveEmptyEntries));
                    return;
                }

                this.AddGame(e.Message.Chat, e.Message.Text.Replace("/", string.Empty).Trim());
            }
            catch (CommandProcessingException commandProcessingException)
            {
                await this.BotClient.SendTextMessageAsync(e.Message.Chat, commandProcessingException.Message);
                //Log.Warn(commandProcessingException);
            }
            catch (Exception exception)
            {
                await this.BotClient.SendTextMessageAsync(e.Message.Chat, CommonError);
                Log.Error(exception);
            }
        }

        #region Main Command Handlers
        /// <summary>
        /// Отправляет меню для начала регистрации игрока.
        /// </summary>
        /// <param name="chat">Чат.</param>
        private async Task SendSignIn(Chat chat)
        {
            string replyMessage = "Для начала пользования ботом необходимо зарегистрироваться";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton("Зарегистрироваться", SignInCommand.ToString()),
            });
            await this.BotClient.SendTextMessageAsync(chat, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Отправляет команды меню бота.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора. Если равно 0, то меню будет отправлено новым сообщением.</param>
        private async Task SendMenu(Chat chat, int messageId = 0)
        {
            string replyMessage = "Выберите действие:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Создать игру", ChooseKindOfSportCommand.ToString()),
                    new InlineKeyboardCallbackButton("Мои игры", MyGamesCommand.ToString())
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Сменить часовой пояс", TimeZoneCommand.ToString())
                } 
            });

            if (messageId != 0)
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
        /// <param name="userId">Идентификатор создателя в Telegram.</param>
        /// <param name="parameters">Массив параметров игры.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private async Task ParseGameParams(Chat chat, int userId, string[] parameters)
        {
            ObjectId gameId;
            lock (this._userGameConfigureSession)
            {
                if (!this._userGameConfigureSession.TryGetValue(userId, out gameId))
                {
                    throw new CommandProcessingException("Режим задания параметров игры не активирован. Для активации выберите игру для редактирования или создайте новую");
                }
            }
            
            if (parameters.All(string.IsNullOrEmpty))
            {
                throw new CommandProcessingException(BadDataTryAgainError);
            }

            Game game;
            try
            {
                game = new Game(gameId,
                                parameters[0],
                                DateTime.ParseExact(parameters[2], DateTimeFormat, CultureInfo.InvariantCulture),
                                int.Parse(parameters[1]));
            }
            catch (Exception exception)
            {
                throw new CommandProcessingException(BadDataError, exception);
            }

            lock (this._userGameConfigureSession)
            {
                this._userGameConfigureSession.Remove(userId);
            }
            this._repository.Update(game);

            await this.BotClient.SendTextMessageAsync(chat, "Чтобы добавить игру в чат, скопируйте сообщение ниже и отправьте в целевой чат.");
            await this.BotClient.SendTextMessageAsync(chat, $"/{game.Id}");
        }

        /// <summary>
        /// Добавляет игру в чат.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="rawGameId">Идентификатор игры.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private void AddGame(Chat chat, string rawGameId)
        {
            if (chat.Type == ChatType.Private)
            {
                throw new CommandProcessingException(OnlyChatAvailable);
            }

            ObjectId gameId;
            if (!ObjectId.TryParse(rawGameId, out gameId))
            {
                throw new CommandProcessingException(BadGameIdError);
            }

            var game = this._repository.Get<Game>(gameId);
            if (game == null)
            {
                throw new CommandProcessingException(GameNotExistError);
            }

            if (!game.IsPublic)
            {
                if (game.ChatId.IsDefault())
                {
                    game.ChatId = chat.Id;
                    this._repository.Update(game);
                }
                else if (game.ChatId != chat.Id)
                {
                    throw new CommandProcessingException(PrivateGameIsAlreadyAdded);
                }
            }

            this.SendGameParamsMessage(chat, 0, game);
        }

        /// <summary>
        /// Формирует сообщение с руководством по использованию бота.
        /// </summary>
        /// <param name="chat">Чат.</param>
        private async void SendFaq(Chat chat)
        {
            string infoMessage = "Руководство по использованию бота."; // ToDO Составить информационное сообщение
            await this.BotClient.SendTextMessageAsync(chat, infoMessage);
        }
        #endregion

        /// <summary>
        /// Обрабатывает событие получения обратного вызова.
        /// </summary>
        /// <param name="sender">Инициатор события.</param>
        /// <param name="e">Параметры события.</param>
        private async void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                string callbackParams;
                var command = e.CallbackQuery.Data.SplitCommandAndParams(CommandAndParamsSplitter, out callbackParams);
                var player = this._repository.Get(new Player(e.CallbackQuery.From.Id)).FirstOrDefault();
                if (player == null)
                {
                    if (command == SignInCommand)
                    {
                        this._repository.Add(new Player(e.CallbackQuery.From.Id)
                        {
                            Name = e.CallbackQuery.From.FirstName,
                            Surname = e.CallbackQuery.From.LastName,
                            Nickname = e.CallbackQuery.From.Username
                        });
                        await this.SendTimeZone(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId,
                            $"{e.CallbackQuery.From.Username}, Вы успешно зарегистрированы. Выберите Ваш часовой пояс:");
                        return;
                    }

                    await this.BotClient.SendTextMessageAsync(e.CallbackQuery.Message.Chat,
                        $"Перед началом пользования ботом необходимо зарегистрироваться. Для регистрации отправьте сообщение {StartCommand} в личный чат с ботом");
                    return;
                }

                switch (command)
                {
                    case MainMenuCommand:
                        lock (this._userGameConfigureSession)
                        {
                            this._userGameConfigureSession.Remove(player.TelegramId);
                        }
                        await this.SendMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                        return;

                    case FinishCommand:
                        lock (this._userGameConfigureSession)
                        {
                            this._userGameConfigureSession.Remove(player.TelegramId);
                        }
                        try
                        {
                            await this.BotClient.DeleteMessageAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                        }
                        catch (Exception)
                        {
                            // No action
                        }
                        return;

                    case TimeZoneCommand:
                        await this.SendTimeZone(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, "Выберите Ваш часовой пояс:", true);
                        return;

                    case ChooseKindOfSportCommand:
                        if (this._userGameConfigureSession.Keys.Contains(player.TelegramId))
                        {
                            return;
                        }
                        lock (this._userGameConfigureSession)
                        {
                            if (!string.IsNullOrEmpty(callbackParams))
                            {
                                this._userGameConfigureSession[player.TelegramId] = ObjectId.Parse(callbackParams);
                            }
                            else
                            {
                                this._userGameConfigureSession[player.TelegramId] = this._repository.Add(new Game(player.Id));
                            }
                        }
                        await this.SendChooseKindOfSportMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                        return;

                    case ChooseGamePrivacyCommand:
                        if (!this._userGameConfigureSession.Keys.Contains(player.TelegramId))
                        {
                            return;
                        }
                        if (this._userGameConfigureSession[player.TelegramId].IsDefault())
                        {
                            throw new CommandProcessingException(BadGameIdError);
                        }
                        if (!string.IsNullOrEmpty(callbackParams))
                        {
                            this._repository.Update(new Game(this._userGameConfigureSession[player.TelegramId],
                                                             (KindOfSport) Enum.Parse(typeof (KindOfSport), callbackParams)));
                        }
                        await this.SendChooseGamePrivacyMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                        return;

                    case NewGameCommand:
                        if (!this._userGameConfigureSession.Keys.Contains(player.TelegramId))
                        {
                            return;
                        }
                        if (this._userGameConfigureSession[player.TelegramId].IsDefault())
                        {
                            throw new CommandProcessingException(BadGameIdError);
                        }
                        if (!string.IsNullOrEmpty(callbackParams))
                        {
                            var gameUpdate = bool.Parse(callbackParams)
                                ? new Game(this._userGameConfigureSession[player.TelegramId], true)
                                : new Game(this._userGameConfigureSession[player.TelegramId], false) {ChatId = 0};
                            this._repository.Update(gameUpdate);
                        }
                        await this.StartWaitingGameParams(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                        return;

                    case JoinFirstCommand:
                        this.JoinGame(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, JoinFirstCommand, ObjectId.Parse(callbackParams), player.Id);
                        return;

                    case JoinSecondCommand:
                        this.JoinGame(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, JoinSecondCommand, ObjectId.Parse(callbackParams), player.Id);
                        return;

                    case DeclineCommand:
                        this.DeclineGame(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, ObjectId.Parse(callbackParams), player.Id);
                        return;

                    case GetGameCodeCommand:
                        await this.GetGameCode(e.CallbackQuery.Message.Chat, ObjectId.Parse(callbackParams));
                        return;

                    case FixGameCommand:
                        lock (this._userGameConfigureSession)
                        {
                            this._userGameConfigureSession[player.TelegramId] = ObjectId.Parse(callbackParams);
                        }
                        await this.SendChooseKindOfSportMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                        return;

                    case SetTimeZoneCommand:
                        await this.SetTimeZone(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, callbackParams);
                        return;

                    case MyGamesCommand:
                    case ToFirstGameCommand:
                        await this.SendGamesViewMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, "1");
                        return;

                    case PreviousGameCommand:
                    case NextGameCommand:
                        await this.SendGamesViewMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, callbackParams);
                        return;

                    case ToLastGameCommand:
                        await this.SendGamesViewMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, "0");
                        return;

                    case DeleteGameCommand:
                        await this.DeleteGame(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, player, callbackParams);
                        return;

                    default:
                        throw new CommandProcessingException(BadDataError);
                }
            }
            catch (CommandProcessingException commandProcessingException)
            {
                await this.BotClient.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, commandProcessingException.Message);
                Log.Warn(commandProcessingException);
            }
            catch (Exception exception)
            {
                await this.BotClient.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, CommonError);
                Log.Error(exception);
            }
        }

        #region Callback Handlers
        /// <summary>
        /// Отправляет меню выбора часового пояса.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="replyMessage">Текст сообщения.</param>
        /// <param name="cancelButtonOn">Флаг необходимости включения кнопки Отмена.</param>
        private async Task SendTimeZone(Chat chat, int messageId, string replyMessage, bool cancelButtonOn = false)
        {
            var timeZoneButtons =
                TimeZone.Select(tz => new InlineKeyboardButton[]
                        {
                            new InlineKeyboardCallbackButton(tz.Value, $"{SetTimeZoneCommand}{CommandAndParamsSplitter}{(int) tz.Key}")
                        })
                        .ToArray();
            var buttons = cancelButtonOn
                ? timeZoneButtons.Concat(new[]
                                 {
                                     new InlineKeyboardButton[]
                                     {
                                         new InlineKeyboardCallbackButton("Отмена", MainMenuCommand.ToString())
                                     }
                                 })
                                 .ToArray()
                : timeZoneButtons;
            await this.BotClient.EditMessageTextAsync(chat, messageId, replyMessage, replyMarkup: new InlineKeyboardMarkup(buttons));
        }

        /// <summary>
        /// Устанавливает смещение от UTC часового пояса игрока.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="player">Игрок.</param>
        /// <param name="timeZoneValue">Значение смещения часового пояса.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private async Task SetTimeZone(Chat chat, int messageId, Player player, string timeZoneValue)
        {
            int timeZone;
            if (!int.TryParse(timeZoneValue, out timeZone))
            {
                throw new CommandProcessingException(BadDataError);
            }

            player.TimeZone = timeZone;
            this._repository.Update(player);
            string timeZoneDescription;
            TimeZone.TryGetValue((TimeZoneOffset) timeZone, out timeZoneDescription);
            timeZoneDescription = !string.IsNullOrEmpty(timeZoneDescription) ? $" ({timeZoneDescription})" : string.Empty;
            await this.BotClient.EditMessageTextAsync(chat, messageId, $"{player}, Ваш часовой пояс{timeZoneDescription} сохранён");
        }

        /// <summary>
        /// Отправляет меню выбора вида спорта.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        private async Task SendChooseKindOfSportMenu(Chat chat, int messageId)
        {
            string replyMessage = "Выберите вид спорта:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(KindOfSports.Select(kind => new InlineKeyboardButton[]
                                                                                   {
                                                                                       new InlineKeyboardCallbackButton(kind.Value.GetElementDescription(),
                                                                                            $"{ChooseGamePrivacyCommand}{CommandAndParamsSplitter}{(int)kind.Value}")
                                                                                   })
                                                                                   .Concat(new[]
                                                                                   {
                                                                                       new InlineKeyboardButton[]
                                                                                       {
                                                                                           new InlineKeyboardCallbackButton("Завершить", MainMenuCommand.ToString()),
                                                                                           new InlineKeyboardCallbackButton("Далее", ChooseGamePrivacyCommand.ToString())
                                                                                       }
                                                                                   })
                                                                                   .ToArray()
                                                                      );
            await this.BotClient.EditMessageTextAsync(chat, messageId, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Отправляет меню выбора приватности игры.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private async Task SendChooseGamePrivacyMenu(Chat chat, int messageId)
        {
            string replyMessage = "Выберите уровень приватности игры:";
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Частная", $"{NewGameCommand}{CommandAndParamsSplitter}{false}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Общедоступная", $"{NewGameCommand}{CommandAndParamsSplitter}{true}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Завершить", MainMenuCommand.ToString()),
                    new InlineKeyboardCallbackButton("Далее", NewGameCommand.ToString())
                }
            });
            await this.BotClient.EditMessageTextAsync(chat, messageId, replyMessage, replyMarkup: inlineReplyMarkup);
        }

        /// <summary>
        /// Задает параметры Игре и сохраняет её в MongoDB.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private async Task StartWaitingGameParams(Chat chat, int messageId)
        {
            IReplyMarkup inlineReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton("Завершить ввод параметров", FinishCommand.ToString())
            });
            string text =
                "Введите название, максимальное количество игроков в команде и дату начала игры в соответствии с приведённым ниже примером:\nНазвание игры\n5\n20:00 06.10.2017";
            await this.BotClient.EditMessageTextAsync(chat, messageId, replyMarkup: inlineReplyMarkup, text: text);
        }

        /// <summary>
        /// Производит присоедиенение к игре.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="joinTo">Указывает к кому присоединиться.</param>
        /// <param name="gameId">Идентификатор Игры.</param>
        /// <param name="playerId">Идентификатор участника.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private void JoinGame(Chat chat, int messageId, CallbackBotCommands joinTo, ObjectId gameId, ObjectId playerId)
        {
            if (gameId.IsDefault())
            {
                throw new CommandProcessingException(BadDataError);
            }
            
            var game = this._repository.Get<Game>(gameId);
            if (game == null)
            {
                throw new CommandProcessingException(GameNotExistError);
            }

            if (!game.IsPublic && game.ChatId != chat.Id)
            {
                throw new CommandProcessingException(PrivateGameIsAlreadyAdded);
            }

            if (game.FirstTeam == null)
            {
                game.FirstTeam = new Team(string.Empty);
            }
            if (game.SecondTeam == null)
            {
                game.SecondTeam = new Team(string.Empty);
            }
            
            switch (joinTo)
            {
                case JoinFirstCommand:
                    if (game.FirstTeam.PlayerIds.Count == game.PlayersPerTeam)
                    {
                        break;
                    }
                    game.SecondTeam.PlayerIds.Remove(playerId);
                    if (game.FirstTeam.PlayerIds.Add(playerId))
                    {
                        this._repository.Replace(game);
                    }
                    break;
                case JoinSecondCommand:
                    if (game.SecondTeam.PlayerIds.Count == game.PlayersPerTeam)
                    {
                        break;
                    }
                    game.FirstTeam.PlayerIds.Remove(playerId);
                    if (game.SecondTeam.PlayerIds.Add(playerId))
                    {
                        this._repository.Replace(game);
                    }
                    break;
                default:
                    return;
            }

            this.SendGameParamsMessage(chat, messageId, game);
        }

        /// <summary>
        /// Производит отказ игрока от игры.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="gameId">Идентификатор Игры.</param>
        /// <param name="playerId">Идентификатор участника.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private void DeclineGame(Chat chat, int messageId, ObjectId gameId, ObjectId playerId)
        {
            if (gameId.IsDefault())
            {
                throw new CommandProcessingException("Некорректные данные");
            }
           
            var game = this._repository.Get<Game>(gameId);
            if (game == null)
            {
                throw new CommandProcessingException("Игра не существует");
            }

            if (!game.IsPublic && game.ChatId != chat.Id)
            {
                throw new CommandProcessingException(PrivateGameIsAlreadyAdded);
            }

            if ((game.FirstTeam?.PlayerIds?.Remove(playerId) ?? false) ||
                (game.SecondTeam?.PlayerIds?.Remove(playerId) ?? false))
            {
                this._repository.Replace(game);
            }

            this.SendGameParamsMessage(chat, messageId, game);
        }

        /// <summary>
        /// Получает команду для добавления игры в чат.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="objectId">Идентификатор игры.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private async Task GetGameCode(Chat chat, ObjectId objectId)
        {
            if (objectId.IsDefault())
            {
                throw new CommandProcessingException("Некорректные данные");
            }
            await this.BotClient.SendTextMessageAsync(chat,
                "Чтобы добавить игру в чат, скопируйте сообщение ниже и отправьте в целевой чат.");
            await this.BotClient.SendTextMessageAsync(chat, $"/{objectId}");
        }
        
        /// <summary>
        /// Добавляет или обновляет сообщение с параметрами игры в чате.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора (если равно нулю, то будет сгенерировано новое сообщение, иначе заменено указанное).</param>
        /// <param name="game">Игра.</param>
        private async void SendGameParamsMessage(Chat chat, int messageId, Game game)
        {
            var creator = this._repository.Get<Player>(game.CreatorId);
            string firstTeamName = string.IsNullOrEmpty(game.FirstTeam?.Name) ? "А" : game.FirstTeam.Name;
            string secondTeamName = string.IsNullOrEmpty(game.SecondTeam?.Name) ? "Б" : game.SecondTeam.Name;
            var restrictedPlayerIds = new List<ObjectId>();
            Func<ObjectId, string> userNameSelector = teamMemberId =>
            {
                var teamMember = this._repository.Get<Player>(teamMemberId);
                var member = this.BotClient.GetChatMemberAsync(chat, teamMember.TelegramId).Result;
                if (member == null ||
                    (member.Status != ChatMemberStatus.Kicked && member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Restricted))
                {
                    return !string.IsNullOrEmpty(teamMember.Nickname) ? $"@{teamMember.Nickname}" : $"{teamMember.Name} {teamMember.Surname}".Trim();
                }

                restrictedPlayerIds.Add(teamMemberId);
                return string.Empty;
            };

            string gameMessage = GenerateGameInfoMessage(game, creator, userNameSelector);
            restrictedPlayerIds.ForEach(id =>
            {
                game.FirstTeam?.PlayerIds?.Remove(id);
                game.SecondTeam?.PlayerIds?.Remove(id);
            });
            this._repository.Replace(game);
            var buttons = new List<InlineKeyboardButton>();
            if (game.FirstTeam?.PlayerIds?.Count != game.PlayersPerTeam)
            {
                buttons.Add(new InlineKeyboardCallbackButton($"За {firstTeamName}", $"{JoinFirstCommand}{CommandAndParamsSplitter}{game.Id}"));
            }
            if (game.SecondTeam?.PlayerIds?.Count != game.PlayersPerTeam)
            {
                buttons.Add(new InlineKeyboardCallbackButton($"За {secondTeamName}", $"{JoinSecondCommand}{CommandAndParamsSplitter}{game.Id}"));
            }

            IReplyMarkup inlineMarkup = new InlineKeyboardMarkup(new[]
            {
                buttons.ToArray(), 
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Отказаться", $"{DeclineCommand}{CommandAndParamsSplitter}{game.Id}")
                }, 
            });

            try
            {
                if (messageId == 0)
                {
                    await this.BotClient.SendTextMessageAsync(chat, gameMessage, replyMarkup: inlineMarkup);
                    return;
                }

                await this.BotClient.EditMessageTextAsync(chat, messageId, gameMessage, replyMarkup: inlineMarkup);
            }
            catch (Exception)
            {
                // No action
            }
        }

        /// <summary>
        /// Удаляет игру.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="player">Игрок.</param>
        /// <param name="deleteParams">Параметры для удаления Игры.</param>
        /// <exception cref="CommandProcessingException"></exception>
        private async Task DeleteGame(Chat chat, int messageId, Player player, string deleteParams)
        {
            int splitterIndex = deleteParams.IndexOf(ParamsSplitter);
            string rawGameId = deleteParams.Substring(0, splitterIndex);
            ObjectId gameId;
            if (!ObjectId.TryParse(rawGameId, out gameId))
            {
                throw new CommandProcessingException(BadGameIdError);
            }

            var game = this._repository.Get<Game>(gameId);
            if (game == null)
            {
                throw new CommandProcessingException(GameNotExistError);
            }
            
            this._repository.Delete<Game>(game.Id);
            await this.SendGamesViewMenu(chat, messageId, player, deleteParams.Remove(0, splitterIndex + 1));
        }

        /// <summary>
        /// Отправляет меню обзора доступных игр.
        /// </summary>
        /// <param name="chat">Чат.</param>
        /// <param name="messageId">Идентификатор сообщения-инициатора.</param>
        /// <param name="player">Игрок.</param>
        /// <param name="number">Текущий порядковый номер игры.</param>
        private async Task SendGamesViewMenu(Chat chat, int messageId, Player player, string number)
        {
            int gameNumber = 0;
            if (!string.IsNullOrEmpty(number) && !int.TryParse(number.Trim(), out gameNumber))
            {
                throw new CommandProcessingException(BadDataError);
            }

            var games = this._repository.Get(new Game(player.Id));
            if (games == null || games.Count == 0)
            {
                await this.BotClient.EditMessageTextAsync(chat, messageId, "У Вас нет игр");
                return;
            }

            Game currentGame;
            var inlineNavigationKeyboard = new List<InlineKeyboardButton>();
            if (games.Count == 1)
            {
                gameNumber = 1;
                currentGame = games.First();
            }
            else if (gameNumber <= 0 || games.Count <= gameNumber)
            {
                gameNumber = games.Count;
                currentGame = games.Last();
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<<", ToFirstGameCommand.ToString()));
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<", $"{PreviousGameCommand}{CommandAndParamsSplitter}{gameNumber - 1}"));
            }
            else if (gameNumber == 1)
            {
                currentGame = games[gameNumber - 1];
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">", $"{NextGameCommand}{CommandAndParamsSplitter}{gameNumber + 1}"));
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">>", ToLastGameCommand.ToString()));
            }
            else
            {
                currentGame = games[gameNumber - 1];
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<<", ToFirstGameCommand.ToString()));
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<", $"{PreviousGameCommand}{CommandAndParamsSplitter}{gameNumber - 1}"));
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">", $"{NextGameCommand}{CommandAndParamsSplitter}{gameNumber + 1}"));
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">>", ToLastGameCommand.ToString()));
            }

            Func<ObjectId, string> userNameSelector = teamMemberId =>
            {
                var teamMember = this._repository.Get<Player>(teamMemberId);
                return !string.IsNullOrEmpty(teamMember.Nickname) ? $"@{teamMember.Nickname}" : $"{teamMember.Name} {teamMember.Surname}".Trim();
            };
            var gameInfoMessage = $"{gameNumber} из {games.Count}{Environment.NewLine}" +
                                  GenerateGameInfoMessage(currentGame, player, userNameSelector);
            IReplyMarkup inlineMarkup = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Получить код", $"{GetGameCodeCommand}{CommandAndParamsSplitter}{currentGame.Id}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Редактировать", $"{FixGameCommand}{CommandAndParamsSplitter}{currentGame.Id}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Удалить", $"{DeleteGameCommand}{CommandAndParamsSplitter}{currentGame.Id}{ParamsSplitter}{gameNumber}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Главное меню", MainMenuCommand.ToString())
                },
                inlineNavigationKeyboard.ToArray()
            });

            try
            {
                if (messageId == 0)
                {
                    await this.BotClient.SendTextMessageAsync(chat, gameInfoMessage, replyMarkup: inlineMarkup);
                    return;
                }
                await this.BotClient.EditMessageTextAsync(chat, messageId, gameInfoMessage, replyMarkup: inlineMarkup);
            }
            catch (Exception)
            {
                // No action
            }
        }
        #endregion

        /// <summary>
        /// Генерирует сообщение с информацией об Игре.
        /// </summary>
        /// <param name="game">Игра.</param>
        /// <param name="creator">Создатель игры.</param>
        /// <param name="userNameSelector">Функция-сборщик списка игроков.</param>
        /// <returns></returns>
        private static string GenerateGameInfoMessage(Game game, Player creator, Func<ObjectId, string> userNameSelector)
        {
            string firstTeamName = string.IsNullOrEmpty(game.FirstTeam?.Name) ? "А" : game.FirstTeam.Name;
            string secondTeamName = string.IsNullOrEmpty(game.SecondTeam?.Name) ? "Б" : game.SecondTeam.Name;
            string gmt = " (" + (creator.TimeZone >= 0 ? $"+{creator.TimeZone}" : creator.TimeZone.ToString()) + " GMT)";
            return 
                string.Format(GameInfoPattern,
                              game.Name,
                              game.KindOfSport.GetElementDescription(),
                              game.IsPublic ? "Общедоступная" : "Частная",
                              game.PlayersPerTeam,
                              game.StartTime.AddHours(creator.TimeZone).ToString(DateTimeFormat)) + gmt + "\n" +
                              (game.FirstTeam != null && game.FirstTeam.PlayerIds.Any()
                                  ? $"Команда {firstTeamName}:\n{string.Join(",", game.FirstTeam.PlayerIds.Select(userNameSelector).Where(nick => !string.IsNullOrEmpty(nick)))}\n"
                                  : $"Команда {firstTeamName}:\n") +
                              (game.SecondTeam != null && game.SecondTeam.PlayerIds.Any()
                                  ? $"Команда {secondTeamName}:\n{string.Join(",", game.SecondTeam.PlayerIds.Select(userNameSelector).Where(nick => !string.IsNullOrEmpty(nick)))}"
                                  : $"Команда {secondTeamName}:\n");
        }
    }
}
