using System.Windows.Media;
using Styx;
using Styx.Common;
using Styx.CommonBot;

namespace Demonic.Helpers
{
    static class Log
    {
        private static string _lastMessage = "";
        private static string _lastMessageToGame = "";

        public static void Info(string message, params object[] args)
        {
            string formatted = TreeRoot.IsRunning
                    ? string.Format("[Demonic] [HP: " + StyxWoW.Me.HealthPercent.ToString("##.#") + "] [Mana: " + StyxWoW.Me.ManaPercent.ToString("##.#") + "] " + message, args)
                    : string.Format("[Demonic] " + message, args);

            if (_lastMessage == formatted) return;
            _lastMessage = formatted;

            Logging.Write(Colors.MediumPurple, formatted);
        }

        public static void Debug(string message, params object[] args)
        {
            Logging.Write(LogLevel.Diagnostic, Colors.Fuchsia, "[Demonic ~ Debug] {0}", string.Format(message, args));
        }

        public static void Fail(string message, params object[] args)
        {
            Logging.Write(LogLevel.Diagnostic, Colors.Crimson, "[Demonic ~ Fail] {0}", string.Format(message, args));
        }

        public static void Orange(string message, params object[] args)
        {
            Logging.Write(Colors.OrangeRed, "[Demonic] {0}", string.Format(message, args));
        }

        public static void Green(string message, params object[] args)
        {
            Logging.Write(LogLevel.Diagnostic, Colors.Green, "[Demonic] {0}", string.Format(message, args));
        }

        public static void LogToGame(string message, params object[] args)
        {
            string formatted = string.Format(message, args).Replace("'", "\'").Replace("!", ".");

            if (_lastMessageToGame == formatted) return;
            _lastMessageToGame = formatted;

            Styx.WoWInternals.Lua.DoString(@"print('\124cff9482C9[Demonic]\124cFFFFFFFF " + formatted + "')");
        }


    }
}
