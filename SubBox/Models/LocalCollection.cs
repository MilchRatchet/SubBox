using SubBox.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace SubBox.Models
{
    public class LocalCollection
    {
        public static Dictionary<string, LocalVideo> DownloadedVideos;

        public static void AddLocalVideo(Video v, string id)
        {
            string dir = string.Empty;

            try { 
                dir = Directory.GetFiles("Videos", id, SearchOption.AllDirectories)[0]; 
            }
            catch (Exception e)
            {
                Logger.Warn("Local Video was not found");

                Logger.Error(e.Message);
            }

            LocalVideo lv = new LocalVideo()
            {
                Data = v,

                Dir = dir
            };

            try
            {
                DownloadedVideos.Add(lv.Data.Id, lv);
            }
            catch (Exception e)
            {
                Logger.Warn("LocalVideo could not be added to Collection");

                Logger.Error(e.Message);
            }
        }

        public static void CollectAllDownloadedVideos()
        {
            DownloadedVideos = new Dictionary<string, LocalVideo>();

            if (!Directory.Exists("Videos"))
            {
                Logger.Warn("No Folder Videos Found");

                return;
            }

            string[] LocalChannels = Directory.GetDirectories("Videos");

            using (AppDbContext context = new AppDbContext())
            {
                foreach (string ch in LocalChannels)
                {
                    string[] LocalVideos = Directory.GetFiles(ch);

                    foreach (string v in LocalVideos)
                    {
                        string id = v.Split('\\')[2].Split('_')[0];

                        Video video = context.Videos.Find(id);
                        
                        if (video==null)
                        {
                            Logger.Warn(id + " is local but not in db");

                            video = new Video()
                            {
                                Id = id,

                                ChannelId = v.Split('\\')[1].Split('_')[0],

                                Title = v.Substring(v.IndexOf('_'))
                            };
                        }

                        LocalVideo lv = new LocalVideo()
                        {
                            Data = video,

                            Dir = @$"Videos\{ch}\{v}"
                        };

                        try
                        {
                            DownloadedVideos.Add(lv.Data.Id, lv);
                        }
                        catch (Exception e)
                        {
                            Logger.Warn("LocalVideo could not be added to Collection");

                            Logger.Error(e.Message);
                        }
                    }
                }
            }

            Logger.Info(DownloadedVideos.Count + " Videos offline available");
        }

    }
}
