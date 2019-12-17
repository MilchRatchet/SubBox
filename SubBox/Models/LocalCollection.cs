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

            using (AppDbContext context = new AppDbContext())
            {
                string[] webmFiles = Directory.GetFiles(@"wwwroot\Videos", "*.webm", SearchOption.AllDirectories);

                string[] jpgFiles = Directory.GetFiles(@"wwwroot\Videos", "*.jpg", SearchOption.AllDirectories);

                foreach (string file in webmFiles)
                {
                    string id = file.Split('\\')[3].Split('&')[0];

                    string thumbDir = "";

                    foreach (string thumb in jpgFiles)
                    {
                        string thumbId = thumb.Split('\\')[3].Split('&')[0];

                        if (thumbId == id)
                        {
                            thumbDir = thumb;

                            break;
                        }
                    }

                    Video video = context.Videos.Find(id);

                    if (video == null)
                    {
                        Logger.Warn(id + " is local but not in db");

                        try
                        {
                            video = new Video()
                            {
                                Id = id,

                                ChannelTitle = file.Split('\\')[2].Split('&')[1].Replace('_', ' '),

                                ChannelPicUrl = @"http://localhost:5000/media/LogoWhite.png",

                                ThumbnailUrl = $@"https://i.ytimg.com/vi/{id}/mqdefault.jpg",

                                Title = file.Split('\\')[3].Split('&')[1].Replace('_', ' ')
                            };
                        }
                        catch (Exception e)
                        {
                            Logger.Warn("Could not add video that is not in db");

                            Logger.Error(e.Message);

                            continue;
                        }
                    }

                    string[] sections = file.Split('\\');

                    string dir = sections[1] + '\\' + sections[2] + '\\' + sections[3];

                    if (thumbDir != "")
                    {
                        sections = thumbDir.Split('\\');

                        thumbDir = sections[1] + '\\' + sections[2] + '\\' + sections[3];
                    } else
                    {
                        thumbDir = "UNAVAILABLE";
                    }

                    LocalVideo lv = new LocalVideo()
                    {
                        Data = video,

                        Dir = dir,

                        ThumbDir = thumbDir,

                        Size = new FileInfo(Directory.GetCurrentDirectory() + '\\' + file).Length
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

            Logger.Info(DownloadedVideos.Count + " Videos offline available");
        }

    }
}
