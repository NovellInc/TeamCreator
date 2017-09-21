using System;
using System.Reflection;
using DataModels.Extensions;
using NLog;
using TelegramBot.NinjectModules;
using TelegramBot.Services;
using Topshelf;
using Topshelf.Ninject;

namespace TelegramBot
{
    /// <summary>
    /// Класс программы.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Описание сервиса.
        /// </summary>
        private static readonly string ServiceDescription;

        /// <summary>
        /// Наименование сервиса.
        /// </summary>
        private static readonly string ServiceName;

        /// <summary>
        /// Журнал событий.
        /// </summary>
        private static readonly Logger ProgramLogger;

        /// <summary>
        /// Инициализирует статичные поля класса.
        /// </summary>
        static Program()
        {
            ProgramLogger = LogManager.GetCurrentClassLogger();
            var assembly = Assembly.GetExecutingAssembly();
            ServiceDescription = $"{assembly.GetAttributeValue<AssemblyDescriptionAttribute, string>(a => a.Description)}" +
                                 $", v{assembly.GetAttributeValue<AssemblyFileVersionAttribute, string>(a => a.Version)}";
            ServiceName = assembly.GetAttributeValue<AssemblyProductAttribute, string>(a => a.Product);
        }

        /// <summary>
        /// Входная точка программы.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            var exitCode = HostFactory.Run(ec =>
            {
                ec.UseNinject(new CommonModule());
                ec.Service<TelegramBotService>(s =>
                {
                    s.ConstructUsingNinject();
                    s.WhenStarted(ws => ws.Start());
                    s.WhenStopped(ws => ws.Stop());
                });
                ec.RunAsLocalSystem();

                ec.SetDescription(ServiceDescription);
                ec.SetDisplayName(ServiceName);
                ec.SetServiceName(ServiceName);

                ec.StartAutomatically();
                ec.EnableServiceRecovery(esr =>
                {
                    esr.RestartService(0);
                    esr.OnCrashOnly();
                });
                ec.OnException(e => ProgramLogger.Fatal(e));
                ec.UseNLog();
            });

            if (exitCode == TopshelfExitCode.Ok)
            {
                return;
            }

            UnhandledExceptionHandler(null, new UnhandledExceptionEventArgs(new Exception(exitCode.ToString()), true));
        }

        /// <summary>
        /// Обрабатывает событие необработанного исключения.
        /// </summary>
        /// <param name="sender">Инициатор события.</param>
        /// <param name="e">Аргументы связанные с событием.</param>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            ProgramLogger.Fatal((Exception)e.ExceptionObject);
        }
    }
}
