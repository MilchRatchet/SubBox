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

                string[] mp4Files = Directory.GetFiles(@"wwwroot\Videos", "*.mp4", SearchOption.AllDirectories);

                string[] videoFiles = new string[webmFiles.Length + mp4Files.Length];

                webmFiles.CopyTo(videoFiles, 0);

                mp4Files.CopyTo(videoFiles, webmFiles.Length);

                webmFiles = null;

                mp4Files = null;

                string[] jpgFiles = Directory.GetFiles(@"wwwroot\Videos", "*.jpg", SearchOption.AllDirectories);

                foreach (string file in videoFiles)
                {
                    string id = file.Split('\\')[3].Split('&')[0];

                    string fileDir = file;

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

                    if (fileDir.Contains('#'))
                    {
                        try
                        {
                            string newFile = fileDir.Replace('#', '_');

                            File.Move(fileDir, newFile);

                            fileDir = newFile;

                            string newThumbDir = thumbDir.Replace('#', '_');

                            File.Move(thumbDir, newThumbDir);

                            thumbDir = newThumbDir;

                            Logger.Warn("removed # from file names");
                        }
                        catch (Exception e)
                        {
                            Logger.Warn("video containing # in title could not be renamed");

                            Logger.Warn("video will not work correctly in browser anyway and will be skipped");

                            Logger.Error(e.Message);

                            continue;
                        }
                    }

                    Video video = context.Videos.Find(id);

                    if (video == null)
                    {
                        Logger.Warn(id + " is local but not in db");

                        try
                        {
                            string title = fileDir.Split('\\')[3].Split('&')[1].Replace('_', ' ');

                            title = title.Substring(0, title.LastIndexOf('.'));

                            video = new Video()
                            {
                                Id = id,

                                ChannelTitle = fileDir.Split('\\')[2].Split('&')[1].Replace('_', ' '),

                                ChannelPicUrl = @"http://localhost:5000/media/LogoWhite.png",

                                ThumbnailUrl = $@"https://i.ytimg.com/vi/{id}/mqdefault.jpg",

                                Title = title,

                                Duration = "NULL"
                            };
                        }
                        catch (Exception e)
                        {
                            Logger.Warn("Could not add video that is not in db");

                            Logger.Error(e.Message);

                            continue;
                        }
                    }

                    string[] sections = fileDir.Split('\\');

                    string dir = sections[1] + '\\' + sections[2] + '\\' + sections[3];

                    if (thumbDir != "")
                    {
                        sections = thumbDir.Split('\\');

                        thumbDir = sections[1] + '\\' + sections[2] + '\\' + sections[3];
                    } 
                    else
                    {
                        thumbDir = "UNAVAILABLE";
                    }

                    long videoSize;

                    try
                    {
                        videoSize = new FileInfo(Directory.GetCurrentDirectory() + '\\' + fileDir).Length;
                    }
                    catch (Exception e)
                    {
                        videoSize = -1;

                        Logger.Warn("could not retrieve file size of video");

                        Logger.Error(e.Message);
                    }


                    LocalVideo lv = new LocalVideo()
                    {
                        Data = video,

                        Dir = dir,

                        ThumbDir = thumbDir,

                        Size = videoSize

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
