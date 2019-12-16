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

            if (!Directory.Exists(@"wwwroot\Videos"))
            {
                Logger.Warn("No Folder Videos in wwwroot Found");

                return;
            }

            string[] LocalChannels = Directory.GetDirectories(@"wwwroot\Videos");

            using (AppDbContext context = new AppDbContext())
            {
                foreach (string ch in LocalChannels)
                {
                    string[] LocalVideos = Directory.GetFiles(ch);

                    foreach (string v in LocalVideos)
                    {
                        string id = v.Split('\\')[3].Split('&')[0];

                        string ext = Path.GetExtension(v.Substring(7));

                        Logger.Error(ext);

                        if (ext != ".webm")
                        {
                            if (ext == ".jpg")
                            {
                                continue;
                            }

                            Logger.Warn("Found non webm/jpg file in Videos");

                            continue;
                        }

                        Video video = context.Videos.Find(id);
                        
                        if (video==null)
                        {
                            Logger.Warn(id + " is local but not in db");

                            try
                            {
                                video = new Video()
                                {
                                    Id = id,

                                    ChannelTitle = v.Split('\\')[2].Substring(v.Split('\\')[2].IndexOf('&') + 1).Replace('_', ' '),

                                    ChannelPicUrl = @"http://localhost:5000/media/LogoWhite.png",

                                    ThumbnailUrl = $@"https://i.ytimg.com/vi/{id}/mqdefault.jpg",

                                    Title = v.Split('\\')[3].Substring(v.Split('\\')[3].IndexOf('&') + 1).Replace('_', ' ')
                                };
                            }
                            catch (Exception e)
                            {
                                Logger.Warn("Could not add video that is not in db");

                                Logger.Error(e.Message);

                                continue;
                            }
                        }

                        LocalVideo lv = new LocalVideo()
                        {
                            Data = video,

                            Dir = v.Substring(7).Remove(v.Length - 12),

                            Size = new FileInfo(Directory.GetCurrentDirectory() + @"\wwwroot\" + v.Substring(7)).Length
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
