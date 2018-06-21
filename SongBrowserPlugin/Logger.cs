using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SongBrowserPlugin
{
    class Logger
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

        public void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + message);
        }

        public void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + message);
        }

        public void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + message);
        }

        public void Exception(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[" + loggerName + " @ " + DateTime.Now.ToString("HH:mm") + "] " + message);
        }

    }
}
