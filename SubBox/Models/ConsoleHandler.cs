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
                    case "-auto": Auto(); break;
                    case "-devm": Devm(); break;
                    case "-logf": Logf(); break;
                    case "-dlsv": Dlsv(); break;
                    case "-stca": Stca(); break;
                    case "-schp": Schp(); break;
                    //case "-potd": Potd(); break;
                    case "-help": Help(); break;  
                    default: Console.WriteLine("Invalid Command, type -help for a list of commands"); break;
                }
            }
        }

        private static void Flth()
        {
            Console.WriteLine("Switching Channel Thumbnails");

            Logger.Warn("This function is legacy code");

            Logger.Warn("This should not be needed anymore");

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

                Console.WriteLine("   Count=" + context.Videos.LongCount());

                Console.WriteLine("   CountInPlaylist=" + context.Videos.Where(v => v.List != 0).LongCount());

                Console.WriteLine("   CountInTrashbin=" + context.Videos.Where(v => v.New == false).LongCount());

                Console.WriteLine("Channels: ");

                Console.WriteLine("   Count=" + context.Channels.LongCount());

                Console.WriteLine("LocalVideos: ");

                Console.WriteLine("   Count=" + LocalCollection.DownloadedVideos.Count);

                long size = 0;

                foreach(KeyValuePair<string,LocalVideo> lv in LocalCollection.DownloadedVideos)
                {
                    size += lv.Value.Size;
                }

                size /= 1024;

                size /= 1024;

                Console.WriteLine("   Size=" + size + "MiB");
            }
        }

        private static void Gcst()
        {
            DataRetriever gc = new DataRetriever();

            if (!AppSettings.GCMode)
            {
                Logger.Warn("GCMode is turned off, executing anyway");

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

        private static void Auto()
        {
            AppSettings.AutoStart = !AppSettings.AutoStart;

            AppSettings.Save();

            Console.WriteLine("AutoStart is now " + ((AppSettings.AutoStart) ? "on" : "off"));
        }

        private static void Devm()
        {
            AppSettings.DevMode = !AppSettings.DevMode;

            AppSettings.Save();
            
            Console.WriteLine("DevMode is now " + ((AppSettings.DevMode) ? "on" : "off"));
        }

        private static void Logf()
        {
            Logger.Info("Dumping Logs...");

            Logger.DumpLog();

            Logger.Info("Done");
        }

        private static void Dlsv()
        {
            Console.WriteLine("Enter link of youtube video");

            string link = Console.ReadLine();

            try
            {
                string id = link.Split("?v=")[1];

                id = id.Split("&t=")[0];

                Downloader.DownloadVideo(id);

                Logger.Info("Successfully downloaded video");
            }
            catch (Exception e)
            {
                Logger.Info("Failed downloading video");

                Logger.Error(e.Message);
            }
        }

        private static void Stca()
        {
            _ = StatusBoard.PrintAllStatus();
        }

        private static void Schp()
        {
            Logger.Info("Syncing channel pictures...");

            Downloader.SyncChannelPictures();

            Logger.Info("Done");
        }

        private static void Potd()
        {
            Downloader.GetPictureOfTheDay();
        }

        private static void Help()
        {
            Console.WriteLine("List of all current commands:");

            Console.WriteLine("-flth | Switches Legacy Thumbnails to Default__ Quality");

            Console.WriteLine("-dbst | Outputs status of database");

            Console.WriteLine("-gcst | Forces garbage collection");

            Console.WriteLine("-auto | Toggle AutoStart");

            Console.WriteLine("-devm | Toggle DevMode");

            Console.WriteLine("-logf | Save log file of this session");

            Console.WriteLine("-dlsv | Download individual video");

            Console.WriteLine("-stca | Outputs all status updates in the back end that were not yet requested by the front end");

            Console.WriteLine("-schp | Sync all channel pictures");

            Console.WriteLine("-potd | Retrieve another picture of the day (DEBUG BUILD ONLY)");

            Console.WriteLine("-help | Shows all commands");
        }

        

    }
}
