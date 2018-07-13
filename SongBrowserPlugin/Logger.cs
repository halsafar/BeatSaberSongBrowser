using System;


namespace SongBrowserPlugin
{
    public class Logger
    {
        private string loggerName;

        public Logger(string _name)
        {
            loggerName = _name;
        }

        public static void StaticLog(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[SongBrowserPlugin @ " + DateTime.Now.ToString("HH:mm") + "] " + message);
        }

        public void Debug(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format(format, args));
        }

        public void Info(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format(format, args));
        }

        public void Warning(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format(format, args));
        }

        public void Error(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format(format, args));
        }

        public void Exception(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + String.Format(format, args));
        }

    }
}
