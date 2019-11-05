using SubBox.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SubBox.Models
{
    public class ConsoleHandler
    {
        public static void HandleConsoleInput()
        {
            while (true)
            {
                string input = Console.ReadLine();

                switch (input)
                {
                    case "-flth": Flth(); break;
                    case "-dbst": Dbst(); break;
                    case "-gcst": Gcst(); break;
                    case "-help": Help(); break; 
                    default: Console.WriteLine("Invalid Command, type -help for a list of commands"); break;
                }
            }
        }

        private static void Flth()
        {
            Console.WriteLine("Switching Channel Thumbnails");

            using (AppDbContext context = new AppDbContext())
            {
                List<Channel> Channels = context.Channels.ToList();

                List<Video> Videos = context.Videos.ToList();

                foreach (Channel ch in Channels)
                {
                    ch.ThumbnailUrl = ch.ThumbnailUrl.Replace("240", "88");

                    context.Channels.Update(ch);
                }

                foreach (Video v in Videos)
                {
                    v.ChannelPicUrl = v.ChannelPicUrl.Replace("240", "88");

                    context.Videos.Update(v);
                }

                context.SaveChanges();
            }

            Console.WriteLine("Done");
        }

        private static void Dbst()
        {
            using (AppDbContext context = new AppDbContext())
            {
                Console.WriteLine("Videos: ");

                Console.WriteLine("   Size=" + context.Videos.LongCount());

                Console.WriteLine("   PlaylistSize=" + context.Videos.Where(v => v.List != 0).LongCount());

                Console.WriteLine("   TrashbinSize=" + context.Videos.Where(v => v.New == false).LongCount());

                Console.WriteLine("Channels: ");

                Console.WriteLine("   Size=" + context.Channels.LongCount());
            }
        }

        private static void Gcst()
        {
            DataRetriever gc = new DataRetriever();

            if (!AppSettings.GCMode)
            {
                AppSettings.GCMode = true;

                gc.GarbageCollector();

                AppSettings.GCMode = false;
            }
            else
            {
                gc.GarbageCollector();
            }

            Console.WriteLine("Done");
        }

        private static void Help()
        {
            Console.WriteLine("List of all current commands:");

            Console.WriteLine("-flth | Switches Legacy Thumbnails to Default__ Quality");

            Console.WriteLine("-dbst | Outputs status of database");

            Console.WriteLine("-gcst | Forces garbage collection");

            Console.WriteLine("-help | Shows all commands");
        }

    }
}
