using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SubBox.Models
{
    public class Logger
    {
        private static List<string> Log = new List<string>();

        public static void Info(string text)
        {
            Console.WriteLine("Info: " + text);

            Log.Add("Info: " + text);
        }

        public static void Warn(string text)
        {
            if (AppSettings.DevMode)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine("Warning: " + text);

                Console.ForegroundColor = ConsoleColor.Gray;
            }

            Log.Add("Warning: " + text);
        }

        public static void Error(string text)
        {
            if (AppSettings.DevMode)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("Error: " + text); 

                Console.ForegroundColor = ConsoleColor.Gray;
            }

            Log.Add("Error: " + text);
        }

        public static async void DumpLog()
        {
            await File.WriteAllLinesAsync($"log_{DateTime.Now.ToFileTime()}.txt", Log);
        }
    }
}
