using System;


namespace SongBrowserPlugin
{
    public class Logger
    {
        private string loggerName;
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

        public void Debug(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public void Info(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public void Warning(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public void Error(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format(format, args));
            ResetForegroundColor();
        }

        public void Exception(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format(format, args));
            ResetForegroundColor();
        }

    }
}
