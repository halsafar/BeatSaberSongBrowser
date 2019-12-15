using System;

namespace SongBrowser.Logging
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error
    }

    public class Logger
    {
        private static readonly string LoggerName = "SongBrowser";
        private static readonly LogLevel LogLevel = LogLevel.Info;
        private static readonly ConsoleColor DefaultFgColor = ConsoleColor.Gray;

        private static void ResetForegroundColor()
        {
            Console.ForegroundColor = DefaultFgColor;
        }

        public static void Trace(string format, params object[] args)
        {
            if (LogLevel > LogLevel.Trace)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[" + LoggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Trace] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public static void Debug(string format, params object[] args)
        {
            Plugin.Log.Debug(String.Format(format, args));
        }

        public static void Info(string format, params object[] args)
        {
            Plugin.Log.Info(String.Format(format, args));
        }

        public static void Log(string format, params object[] args)
        {
            Logger.Debug(format, args);
        }

        public static void Warning(string format, params object[] args)
        {
            Plugin.Log.Warn(String.Format(format, args));
        }

        public static void Error(Exception e)
        {
            Logger.Error("{0}", e.Message);
        }

        public static void Error(string format, params object[] args)
        {
            Plugin.Log.Error(String.Format(format, args));
        }

        public static void Exception(string message)
        {
            Plugin.Log.Critical(message);
        }

        public static void Exception(Exception e)
        {
            Plugin.Log.Critical(e);
        }

        public static void Exception(string message, Exception e)
        {
            Plugin.Log.Warn($"{message}:{e}");
        }
    }
}
