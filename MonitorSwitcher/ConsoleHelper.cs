using System;

namespace MonitorSwitcher
{
    public class ConsoleHelper
    {
        public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
        public ConsoleColor InfoColor { get; set; } = ConsoleColor.White;
        public ConsoleColor StatusColor { get; set; } = ConsoleColor.Green;
        public void WriteError(string s)
        {
            WriteColor(s, ErrorColor);
        }

        public void WriteStatus(string s)
        {
            WriteColor(s, StatusColor);
        }

        public void WriteInfo(string s)
        {
            WriteColor(s, InfoColor);
        }

        public void WriteColor(string s, ConsoleColor consoleColor)
        {
            var c = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = consoleColor;
                Console.Write(s);
            }
            finally
            {
                Console.ForegroundColor = c;
            }

        }

    }
}
