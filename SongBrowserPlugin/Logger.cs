using System;


namespace SongBrowserPlugin
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
        private string loggerName;
        private LogLevel _LogLevel = LogLevel.Trace;
        private ConsoleColor _defaultFgColor;

        public Logger(string _name)
        {
            loggerName = _name;
            _defaultFgColor = ConsoleColor.Gray;
        }

        public void ResetForegroundColor()
        {
            Console.ForegroundColor = _defaultFgColor;
        }

        public void Trace(string format, params object[] args)
        {
            if (_LogLevel > LogLevel.Trace)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Trace] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public void Debug(string format, params object[] args)
        {
            if (_LogLevel > LogLevel.Debug)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Debug] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public void Info(string format, params object[] args)
        {
            if (_LogLevel > LogLevel.Info)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Info] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public void Warning(string format, params object[] args)
        {
            if (_LogLevel > LogLevel.Warn)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Warning] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public void Error(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + " - Error] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public void Exception(string message, Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format("{0}-{1}\n{2}", message, e.Message, e.StackTrace));
            ResetForegroundColor();
        }

    }
}
