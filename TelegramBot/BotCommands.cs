namespace TelegramBot
{
    /// <summary>
    /// Команды бота.
    /// </summary>
    public static class BotCommands
    {
        /// <summary>
        /// Команда начала работы с ботом.
        /// </summary>
        public const string StartCommand = "/start";

        /// <summary>
        /// Команда вызова главное меню.
        /// </summary>
        public const string MenuCommand = "/menu";

        /// <summary>
        /// Команда вызова справки.
        /// </summary>
        public const string FaqCommand = "/faq";

        /// <summary>
        /// Команда завершения процесса ввода настроек.
        /// </summary>
        public const string FinishCommand = "/finish";

        /// <summary>
        /// Команда "назад".
        /// </summary>
        public const string BackCommand = "/back";

        /// <summary>
        /// Команда на регистрацию.
        /// </summary>
        public const string SignInCommand = "/signin";

        /// <summary>
        /// Команда на получение меню выбора часового пояса.
        /// </summary>
        public const string TimeZoneCommand = "/timezone";

        /// <summary>
        /// Команда на получение меню выбора вида спорта.
        /// </summary>
        public const string ChooseKindOfSportCommand = "/choosesport";

        /// <summary>
        /// Команда на получение меню выбора приватности игры.
        /// </summary>
        public const string ChooseGamePrivacyCommand = "/choosegameprivacy";

        /// <summary>
        /// Запрос на создание новой игры.
        /// </summary>
        public const string NewGameCommand = "/newgame";

        /// <summary>
        /// Команда на установку времени игры.
        /// </summary>
        public const string SetGameTimeCommand = "/settime";

        /// <summary>
        /// Запрос на получение команды для добавления игры в чат.
        /// </summary>
        public const string GetGameCodeCommand = "/gamecode";

        /// <summary>
        /// Команда на получение меню первой команды списка.
        /// </summary>
        public const string ToFirstGameCommand = "/tofirst";

        /// <summary>
        /// Команда на получение меню предыдущей команды списка.
        /// </summary>
        public const string PreviousGameCommand = "/previous";

        /// <summary>
        /// Команда на получение меню следующей команды списка.
        /// </summary>
        public const string NextGameCommand = "/next";

        /// <summary>
        /// Команда на получение меню последней команды списка.
        /// </summary>
        public const string ToLastGameCommand = "/tolast";

        /// <summary>
        /// Команда на получение меню редактирования игры.
        /// </summary>
        public const string FixGameCommand = "/fixgame";

        /// <summary>
        /// Команда на удаление игры.
        /// </summary>
        public const string DeleteGameCommand = "/deletegame";

        /// <summary>
        /// Команда на добавление игры в чат.
        /// </summary>
        public const string AddGameCommand = "/addgame";

        /// <summary>
        /// Команда на получение списка созданных игр.
        /// </summary>
        public const string MyGamesCommand = "/mygames";

        /// <summary>
        /// Команда на присоединение к первой команде в контексте игры.
        /// </summary>
        public const string JoinFirstCommand = "joinfirst";

        /// <summary>
        /// Команда на присоединение ко второй команде в контексте игры.
        /// </summary>
        public const string JoinSecondCommand = "joinsecond";

        /// <summary>
        /// Команда для отказа участвовать в игре.
        /// </summary>
        public const string DeclineCommand = "decline";
    }
}
