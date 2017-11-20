using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dal.Interfaces;
using DataModels.Enums;
using DataModels.Models;
using Extensions;
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
        private const string GameInfoPattern = "Название: {0}\nВид спорта: {1}\nДоступность: {2}\nИгроков в команде: {3}\nНачало: {4}";

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
        /// Таймеры оповещателей о начале игры через час.
        /// </summary>
        private readonly IDictionary<ObjectId, Timer> _notifierTimers;

        /// <summary>
        /// Хранилище.
        /// </summary>
        private readonly IMongoRepository _repository;

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
        public TelegramBotService(string token, IMongoRepository repository)
        {
            this._notifierTimers = new Dictionary<ObjectId, Timer>();
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
                if (e.Message.Text.StartsWith(GuideCommand, StringComparison.OrdinalIgnoreCase))
                {
                    await this.SendGuide(e.Message.Chat);
                    return;
                }

                var player = this._repository.Get(new Player(e.Message.From.Id)).Items.FirstOrDefault();
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

                    await this.ParseGameParams(e.Message.Chat,
                        e.Message.From.Id,
                        e.Message.Text.Trim()
                            .Split(new[] {"\n", $"{Environment.NewLine}"}, StringSplitOptions.RemoveEmptyEntries));
                    return;
                }

                await this.AddGame(e.Message.Chat, e.Message.Text.Replace("/", string.Empty).Trim());
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

            var game = this._repository.Get<Game>(gameId);
            try
            {
                int playersPerTeam = int.Parse(parameters[1]);
                if (playersPerTeam < 1)
                {
                    throw new CommandProcessingException(BadDataError);
                }
                game.Name = parameters[0];
                game.StartTime = DateTime.ParseExact(parameters[2], DateTimeFormat, CultureInfo.InvariantCulture);
                game.PlayersPerTeam = playersPerTeam;
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
        private async Task AddGame(Chat chat, string rawGameId)
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

            await this.SendGameParamsMessage(chat,
                                             0,
                                             game,
                                             game.HasFirstTeam ? this._repository.Get<Team>(game.FirstTeamId) : null,
                                             game.HasSecondTeam ? this._repository.Get<Team>(game.SecondTeamId) : null);
        }

        /// <summary>
        /// Формирует сообщение с руководством по использованию бота.
        /// </summary>
        /// <param name="chat">Чат.</param>
        private async Task SendGuide(Chat chat)
        {
            string infoMessage =
                "Руководство по использованию бота.\n" +
                "ОБРАТИТЕ ВНИМАНИЕ. Работа с ботом ведется ТОЛЬКО В ЛИЧНОМ ЧАТЕ!\n" +
                $"1. Перед началом использования необходимо зарегистрироваться. Для этого нужно отправить команду {StartCommand}. " +
                    "В ответ придёт сообщение, где потребуется выбрать Ваш часовой пояс. Это необходимо для последующего правильного отображения времени начала игр. " +
                    "Время начала выводится в часовом поясе создателя игры.\n" +
                $"2. После прохождения регистрации бот станет отвечать на команду запроса меню {MenuCommand}.\n" +
                "3. В ответ на запрос меню придёт сообщение с клавиатурой \"Создать игру\", \"Мои игры\", \"Сменить часовой пояс\".\n" +
                "3.1. \"Создать игру\".\n" +
                    "Эта команда позволяет начать создание игры. Последовательно будет предложено выбрать параметры игры: вид спорта, доступность. " +
                    "На последнем шаге нужно ввести вручную 3 параметра (по одному на строке): название игры (любая фраза), максимальное количество игроков в команде (целое число больше нуля) " +
                    "и время и дату начала игры в формате [чч:мм дд.мм.гггг] (дату и время необходимо разделить пробелом). " +
                    "После успешного ввода параметров придёт сообщение с командой для добавления игры в группу (п. 5). " +
                    "На каждом этапе можно прервать настройку игры и вернуться в главное меню командой \"Завершить\". При этом уже введённые параметры будут сохранены. " +
                    "Как найти созданную игру см. п. 3.2.\n" +
                "3.2. \"Мои игры\".\n" +
                "Эта команда позволяет просматривать параметры созданных Вами игр.\n" +
                "3.2.1. \"Получить код\" возвращает команду для добавления игры в группу (п. 5).\n" +
                "3.2.2. \"Редактировать\" позволяет выполнить те же шаги, что и в п. 3.1. В этом случае изменения будут вноситься в выбранную игру.\n" +
                "3.2.3. \"Удалить\" удаляет игру из списка игр.\n" +
                "3.2.4. \"Главное меню\" возвращает главное меню.\n" +
                "3.2.5. Клавиши навигации по списку игр: \"<<\" - к первой, \"<\" - на одну назад, \">\" - на одну вперёд, \">>\" - к последней.\n" +
                "4. Команда \"Сменить часовой пояс\" позволяет сменить Ваш часовой пояс и возвращает меню со списком часовых поясов.\n" +
                "5. Чтобы начать набирать игроков на игру необходимо добавить игру в какую-либо группу. " +
                    "Для этого нужно скопировать сообщение с командой для добавления игры, полученное в п. 3.1 или 3.2.1 и отправить в целевую группу. " +
                    "Добавить игру можно ТОЛЬКО В ГРУППУ. Для игр с уровнем доступности \"Частная\" при заполнении команд за час до начала игры придёт оповещение в группу.\n" +
                "6. \"Частная\" игра может быть добавлена только в ОДНОЙ группе. Чтобы стало возможным добавлять игру в несколько групп, необходимо установить уровень доступности \"Общедоступная\".";
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
                var player = this._repository.Get(new Player(e.CallbackQuery.From.Id)).Items.FirstOrDefault();
                if (player == null)
                {
                    if (command == SignInCommand)
                    {
                        this._repository.Add(new Player(e.CallbackQuery.From.Id)
                        {
                            TelegramName = e.CallbackQuery.From.FirstName,
                            TelegramSurname = e.CallbackQuery.From.LastName,
                            TelegramNickname = e.CallbackQuery.From.Username,
                            TelegramLanguageCode = e.CallbackQuery.From.LanguageCode
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
                            await this.SendMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
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
                            await this.SendMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                        }
                        if (this._userGameConfigureSession[player.TelegramId].IsDefault())
                        {
                            throw new CommandProcessingException(BadGameIdError);
                        }
                        if (!string.IsNullOrEmpty(callbackParams))
                        {
                            var game = this._repository.Get<Game>(this._userGameConfigureSession[player.TelegramId]);
                            game.KindOfSport = (KindOfSport) Enum.Parse(typeof (KindOfSport), callbackParams);
                            this._repository.Update(game);
                        }
                        await this.SendChooseGamePrivacyMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                        return;

                    case NewGameCommand:
                        if (!this._userGameConfigureSession.Keys.Contains(player.TelegramId))
                        {
                            await this.SendMenu(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                        }
                        if (this._userGameConfigureSession[player.TelegramId].IsDefault())
                        {
                            throw new CommandProcessingException(BadGameIdError);
                        }
                        if (!string.IsNullOrEmpty(callbackParams))
                        {
                            var game = this._repository.Get<Game>(this._userGameConfigureSession[player.TelegramId]);
                            game.IsPublic = bool.Parse(callbackParams);
                            if (game.IsPublic)
                            {
                                game.ChatId = 0;
                            }
                            this._repository.Update(game);
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

            player.TelegramTimeZone = timeZone;
            this._repository.Update(player);
            string timeZoneDescription;
            TimeZone.TryGetValue((TimeZoneOffset) timeZone, out timeZoneDescription);
            timeZoneDescription = !string.IsNullOrEmpty(timeZoneDescription) ? $" ({timeZoneDescription})" : string.Empty;
            string name = string.IsNullOrEmpty(player.TelegramNickname) ? player.TelegramName : player.TelegramNickname;
            await this.BotClient.EditMessageTextAsync(chat, messageId, $"{name}, Ваш часовой пояс{timeZoneDescription} сохранён");
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

            Team firstTeam;
            if (game.HasFirstTeam)
            {
                firstTeam = this._repository.Get<Team>(game.FirstTeamId);
            }
            else
            {
                firstTeam = new Team("А");
                firstTeam.Id = game.FirstTeamId = this._repository.Add(firstTeam);
            }

            Team secondTeam;
            if (game.HasSecondTeam)
            {
                secondTeam = this._repository.Get<Team>(game.SecondTeamId);
            }
            else
            {
                secondTeam = new Team("Б");
                secondTeam.Id = game.SecondTeamId = this._repository.Add(secondTeam);
            }
            
            switch (joinTo)
            {
                case JoinFirstCommand:
                    if (firstTeam.PlayerIds.Count == game.PlayersPerTeam)
                    {
                        break;
                    }
                    secondTeam.PlayerIds.Remove(playerId);
                    firstTeam.PlayerIds.Add(playerId);
                    break;
                case JoinSecondCommand:
                    if (secondTeam.PlayerIds.Count == game.PlayersPerTeam)
                    {
                        break;
                    }
                    firstTeam.PlayerIds.Remove(playerId);
                    secondTeam.PlayerIds.Add(playerId);
                    break;
                default:
                    return;
            }

            this.SendGameParamsMessage(chat, messageId, game, firstTeam, secondTeam).Wait();
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

            Team firstTeam = null;
            if (game.HasFirstTeam)
            {
                firstTeam = this._repository.Get<Team>(game.FirstTeamId);
                firstTeam.PlayerIds?.Remove(playerId);
            }

            Team secondTeam = null;
            if (game.HasSecondTeam)
            {
                secondTeam = this._repository.Get<Team>(game.SecondTeamId);
                secondTeam.PlayerIds?.Remove(playerId);
            }

            this.SendGameParamsMessage(chat, messageId, game, firstTeam, secondTeam).Wait();
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
        /// <param name="firstTeam">Первая команда.</param>
        /// <param name="secondTeam">Вторая команда.</param>
        private async Task SendGameParamsMessage(Chat chat, int messageId, Game game, Team firstTeam, Team secondTeam)
        {
            var creator = this._repository.Get<Player>(game.CreatorId);
            var restrictedPlayerIds = new List<ObjectId>();
            Func<ObjectId, string> userNameSelector = teamMemberId =>
            {
                var teamMember = this._repository.Get<Player>(teamMemberId);
                if (teamMember == null)
                {
                    restrictedPlayerIds.Add(teamMemberId);
                    return string.Empty;
                }

                var member = this.BotClient.GetChatMemberAsync(chat, teamMember.TelegramId).Result;
                if (member == null ||
                    (member.Status != ChatMemberStatus.Kicked && member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Restricted))
                {
                    string telegramLink = teamMember.GetTelegramLink();
                    return !string.IsNullOrEmpty(telegramLink) ? telegramLink : teamMember.ToString();
                }

                restrictedPlayerIds.Add(teamMemberId);
                return string.Empty;
            };

            if (restrictedPlayerIds.Count > 0)
            {
                restrictedPlayerIds.ForEach(id =>
                {
                    firstTeam?.PlayerIds?.Remove(id);
                    secondTeam?.PlayerIds?.Remove(id);
                });
                this._repository.Update(firstTeam);
                this._repository.Update(secondTeam);
            }
            bool switchNotifierTimer = true;
            var buttons = new List<InlineKeyboardButton>();
            if (firstTeam?.PlayerIds.Count != game.PlayersPerTeam)
            {
                switchNotifierTimer = false;
                buttons.Add(new InlineKeyboardCallbackButton($"За {(string.IsNullOrEmpty(firstTeam?.Name) ? "А" : firstTeam.Name)}", $"{JoinFirstCommand}{CommandAndParamsSplitter}{game.Id}"));
            }
            if (secondTeam?.PlayerIds.Count != game.PlayersPerTeam)
            {
                switchNotifierTimer = false;
                buttons.Add(new InlineKeyboardCallbackButton($"За {(string.IsNullOrEmpty(secondTeam?.Name) ? "Б" : secondTeam.Name)}", $"{JoinSecondCommand}{CommandAndParamsSplitter}{game.Id}"));
            }

            var timeLeft = game.StartTime.Subtract(DateTime.UtcNow.AddMinutes(60));
            if (switchNotifierTimer && !game.IsPublic && !this._notifierTimers.Keys.Contains(game.Id) && timeLeft.TotalHours > 1)
            {
                this._notifierTimers[game.Id] = new Timer(this.GameStartNotifier,
                                                          game,
                                                          (int)timeLeft.TotalMilliseconds,
                                                          Timeout.Infinite);
            }

            string gameMessage = GenerateGameInfoMessage(game, creator, firstTeam, secondTeam, userNameSelector);
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

            if (this._notifierTimers.Keys.Contains(game.Id))
            {
                this._notifierTimers[game.Id].Dispose();
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
            if (games.TotalItemsCount == 0)
            {
                await this.BotClient.EditMessageTextAsync(chat, messageId, "У Вас нет игр");
                return;
            }

            Game currentGame;
            var inlineNavigationKeyboard = new List<InlineKeyboardButton>();
            if (games.TotalItemsCount == 1)
            {
                gameNumber = 1;
                currentGame = games.Items.First();
            }
            else if (gameNumber <= 0 || games.TotalItemsCount <= gameNumber)
            {
                gameNumber = games.TotalItemsCount - 1;
                currentGame = games.Items.Last();
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<<", ToFirstGameCommand.ToString()));
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton("<", $"{PreviousGameCommand}{CommandAndParamsSplitter}{gameNumber - 1}"));
            }
            else if (gameNumber == 1)
            {
                currentGame = games.Items.First();
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">", $"{NextGameCommand}{CommandAndParamsSplitter}{gameNumber + 1}"));
                inlineNavigationKeyboard.Add(new InlineKeyboardCallbackButton(">>", ToLastGameCommand.ToString()));
            }
            else
            {
                currentGame = games.Items.ElementAt(gameNumber - 1);
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
            var gameInfoMessage = $"{gameNumber} из {games.TotalItemsCount}{Environment.NewLine}" +
                                  GenerateGameInfoMessage(currentGame,
                                                          player,
                                                          currentGame.HasFirstTeam ? this._repository.Get<Team>(currentGame.FirstTeamId) : null,
                                                          currentGame.HasSecondTeam ? this._repository.Get<Team>(currentGame.SecondTeamId) : null,
                                                          userNameSelector);
            IReplyMarkup inlineMarkup = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Получить код", $"{GetGameCodeCommand}{CommandAndParamsSplitter}{currentGame.Id}")
                },
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton("Редактировать", $"{ChooseKindOfSportCommand}{CommandAndParamsSplitter}{currentGame.Id}")
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
        /// <param name="firstTeam">Первая команда.</param>
        /// <param name="secondTeam">Вторая команда.</param>
        /// <param name="userNameSelector">Функция-сборщик списка игроков.</param>
        /// <returns></returns>
        private static string GenerateGameInfoMessage(Game game, TelegramUser creator, Team firstTeam, Team secondTeam, Func<ObjectId, string> userNameSelector)
        {
            string firstTeamName = string.IsNullOrEmpty(firstTeam?.Name) ? "А" : firstTeam.Name;
            string secondTeamName = string.IsNullOrEmpty(secondTeam?.Name) ? "Б" : secondTeam.Name;
            string gmt = " (GMT " + (creator.TelegramTimeZone >= 0 ? $"+{creator.TelegramTimeZone}" : creator.TelegramTimeZone.ToString()) + ")";
            return 
                string.Format(GameInfoPattern,
                              game.Name,
                              game.KindOfSport.GetElementDescription(),
                              game.IsPublic ? "Общедоступная" : "Частная",
                              game.PlayersPerTeam,
                              game.StartTime.AddHours(creator.TelegramTimeZone).ToString(DateTimeFormat)) + gmt + "\n" +
                              (firstTeam != null && firstTeam.PlayerIds.Any()
                                  ? $"Команда {firstTeamName}:\n{string.Join(",", firstTeam.PlayerIds.Select(userNameSelector).Where(nick => !string.IsNullOrEmpty(nick)))}\n"
                                  : $"Команда {firstTeamName}:\n") +
                              (secondTeam != null && secondTeam.PlayerIds.Any()
                                  ? $"Команда {secondTeamName}:\n{string.Join(",", secondTeam.PlayerIds.Select(userNameSelector).Where(nick => !string.IsNullOrEmpty(nick)))}"
                                  : $"Команда {secondTeamName}:\n");
        }

        /// <summary>
        /// Отправляет сообщение с оповещением о скором начале игры.
        /// </summary>
        /// <param name="gameObj">Объект игры.</param>
        private void GameStartNotifier(object gameObj)
        {
            var game = gameObj as Game;
            if (game == null)
            {
                return;
            }

            this._notifierTimers[game.Id].Dispose();

            Func<ObjectId, string> userNameSelector = teamMemberId =>
            {
                var teamMember = this._repository.Get<Player>(teamMemberId);
                string telegramLink = teamMember.GetTelegramLink();
                return !string.IsNullOrEmpty(telegramLink) ? telegramLink : teamMember.ToString();
            };

            string[] players = new string[0];
            if (game.HasFirstTeam)
            {
                players = this._repository.Get<Team>(game.FirstTeamId).PlayerIds.Select(userNameSelector).ToArray();
            }
            
            if (game.HasSecondTeam)
            {
                players = players.Concat(this._repository.Get<Team>(game.SecondTeamId).PlayerIds.Select(userNameSelector))
                                 .Where(name => !string.IsNullOrEmpty(name))
                                 .ToArray();
            }

            if (players.Length == 0)
            {
                this._notifierTimers[game.Id].Dispose();
                this._notifierTimers.Remove(game.Id);
                return;
            }

            string playersList = string.Join(",", players);
            try
            {
                var creator = this._repository.Get<Player>(game.CreatorId);
                string gmt = " (GMT " + (creator.TelegramTimeZone >= 0 ? $"+{creator.TelegramTimeZone}" : creator.TelegramTimeZone.ToString()) + ")";
                this.BotClient.SendTextMessageAsync(game.ChatId, $"{playersList} вы приглашены на игру \"{game.Name}\". Начало в {game.StartTime.AddHours(creator.TelegramTimeZone).ToString(DateTimeFormat) + gmt}.");
            }
            catch (Exception e)
            {
                Log.Error($"Ошибка при отправке оповещения о начале игры: {e}.");
            }

            this._notifierTimers[game.Id].Dispose();
            this._notifierTimers.Remove(game.Id);
        }
    }
}