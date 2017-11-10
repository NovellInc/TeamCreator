namespace TelegramBot.Models
{
    /// <summary>
    /// Команды обратного вызова бота.
    /// </summary>
    public enum CallbackBotCommands
    {
        /// <summary>
        /// Неизвестная команда.
        /// </summary>
        UnknownCommand = 0, 

        /// <summary>
        /// Команда на регистрацию.
        /// </summary>
        SignInCommand = 1,

        /// <summary>
        /// Команда вернуться в главное меню.
        /// </summary>
        MainMenuCommand = 2,

        /// <summary>
        /// Команда завершения процесса ввода настроек.
        /// </summary>
        FinishCommand = 3,

        /// <summary>
        /// Команда на получение меню выбора часового пояса.
        /// </summary>
        TimeZoneCommand = 4,

        /// <summary>
        /// Команда на установку часового пояса.
        /// </summary>
        SetTimeZoneCommand = 5,

        /// <summary>
        /// Команда на получение меню выбора вида спорта.
        /// </summary>
        ChooseKindOfSportCommand = 6,

        /// <summary>
        /// Команда на получение меню выбора приватности игры.
        /// </summary>
        ChooseGamePrivacyCommand = 7,

        /// <summary>
        /// Запрос на создание новой игры.
        /// </summary>
        NewGameCommand = 8,
        
        /// <summary>
        /// Запрос на получение команды для добавления игры в чат.
        /// </summary>
        GetGameCodeCommand = 9,

        /// <summary>
        /// Команда на получение меню первой команды списка.
        /// </summary>
        ToFirstGameCommand = 10,

        /// <summary>
        /// Команда на получение меню предыдущей команды списка.
        /// </summary>
        PreviousGameCommand = 11,

        /// <summary>
        /// Команда на получение меню следующей команды списка.
        /// </summary>
        NextGameCommand = 12,

        /// <summary>
        /// Команда на получение меню последней команды списка.
        /// </summary>
        ToLastGameCommand = 13,

        /// <summary>
        /// Команда на получение меню редактирования игры.
        /// </summary>
        FixGameCommand = 14,

        /// <summary>
        /// Команда на удаление игры.
        /// </summary>
        DeleteGameCommand = 15,
        
        /// <summary>
        /// Команда на получение списка созданных игр.
        /// </summary>
        MyGamesCommand = 16,

        /// <summary>
        /// Команда на присоединение к первой команде в контексте игры.
        /// </summary>
        JoinFirstCommand = 17,

        /// <summary>
        /// Команда на присоединение ко второй команде в контексте игры.
        /// </summary>
        JoinSecondCommand = 18,

        /// <summary>
        /// Команда для отказа участвовать в игре.
        /// </summary>
        DeclineCommand = 19
    }
}
