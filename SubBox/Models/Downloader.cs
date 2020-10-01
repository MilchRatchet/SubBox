using Newtonsoft.Json;
using SubBox.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace SubBox.Models
{
    public class Downloader
    {
        public static void DownloadFiles()
        {
            try
            {
                bool youtubedl = File.Exists("youtube-dl.exe");

                bool ffmpeg = File.Exists("ffmpeg.exe");

                if (youtubedl && ffmpeg)
                {
                    AppSettings.DownloadReady = true;

                    Logger.Info("Downloading videos is now available");

                    return;
                }

                if (!youtubedl)
                {
                    Logger.Info("youtube-dl.exe not found");

                    Logger.Info("Downloading youtube-dl.exe...");

                    var client = new WebClient();

                    int Progress = 0;

                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        if (e.ProgressPercentage >= Progress)
                        {
                            Progress += 10;

                            Console.WriteLine("youtube-dl.exe: downloaded {1} of {2} bytes. {3} % complete...",
                            (string)e.UserState,
                             e.BytesReceived,
                             e.TotalBytesToReceive,
                             e.ProgressPercentage);
                        }
                    };

                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        youtubedl = true;

                        if (youtubedl && ffmpeg)
                        {
                            AppSettings.DownloadReady = true;

                            Logger.Info("Downloading videos is now available");
                        }
                    };

                    client.DownloadFileAsync(new Uri("https://youtube-dl.org/downloads/latest/youtube-dl.exe"), "youtube-dl.exe");
                }

                if (!ffmpeg)
                {
                    Logger.Info("ffmpeg.exe not found");

                    Logger.Info("Downloading ffmpeg.exe...");

                    var client = new WebClient();

                    int Progress = 0;

                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        if (e.ProgressPercentage >= Progress)
                        {
                            Progress += 10;

                            Console.WriteLine("ffmpeg.exe: downloaded {1} of {2} bytes. {3} % complete...",
                                (string)e.UserState,
                                 e.BytesReceived,
                                 e.TotalBytesToReceive,
                                 e.ProgressPercentage);
                        }

                    };

                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        if (Directory.Exists("ffmpeg"))
                        {
                            Logger.Warn("ffmpeg dir exists unexpectedly");

                            Directory.Delete("ffmpeg", true);
                        }

                        ZipFile.ExtractToDirectory("ffmpeg.zip", "ffmpeg");

                        File.Delete("ffmpeg.zip");

                        try
                        {
                            string dir = Directory.GetDirectories(Directory.GetCurrentDirectory() + @"\ffmpeg")[0];

                            File.Move(dir + @"\bin\ffmpeg.exe", Directory.GetCurrentDirectory() + @"\ffmpeg.exe");

                            Directory.Delete("ffmpeg", true);

                            ffmpeg = true;

                            if (youtubedl && ffmpeg)
                            {
                                AppSettings.DownloadReady = true;

                                Logger.Info("Downloading videos is now available");
                            }
                        }
                        catch (Exception m)
                        {
                            Logger.Info("Failed installing ffmpeg");

                            Logger.Info("Retry or install manually");

                            Logger.Error(m.Message);
                        }
                    };

                    client.DownloadFileAsync(new Uri("https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-4.2.1-win64-static.zip"), "ffmpeg.zip");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);

                Logger.Warn("Failed to download ffmpeg/youtube-dl");
            }
        }

        public static void SyncChannelPictures()
        {
            List<Channel> Channels;

            using (AppDbContext context = new AppDbContext())
            {
                Channels = context.Channels.ToList();
            }

            Directory.CreateDirectory("wwwroot/channelPictures");

            using (var client = new WebClient())
            {
                foreach(Channel ch in Channels)
                {
                    try
                    {
                        client.DownloadFile(new Uri(ch.ThumbnailUrl), "wwwroot/channelPictures/" + ch.Id + ".jpg");
                    }
                    catch (Exception e)
                    {
                        Logger.Error("At Downloader.SyncChannelPictures()");

                        Logger.Error(e.Message);
                    }  
                }     
            }
        }

        public static void AddChannelPicture(Channel ch)
        {
            Directory.CreateDirectory("wwwroot/channelPictures");

            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFile(new Uri(ch.ThumbnailUrl), "wwwroot/channelPictures/" + ch.Id + ".jpg");
                }
                catch (Exception e)
                {
                    Logger.Error("At Downloader.AddChannelPicture(Channel)");

                    Logger.Error(e.Message);
                }
            }
        }

        public static void RemoveChannelPicture(string id)
        {
            try
            {
                File.Delete("wwwroot/channelPictures/" + id + ".jpg");
            }
            catch (Exception e)
            {
                Logger.Error("At Downloader.RemoveChannelPicture(string)");

                Logger.Error(e.Message);
            }
        }

        public static async void GetPictureOfTheDay()
        {
            try
            {
                HttpClient client = new HttpClient();

                HttpResponseMessage response = await client.GetAsync($@"https://api.unsplash.com/photos/random?client_id={Config.UnsplashAPIKey}&orientation=portrait&query=sunset");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();

                    dynamic json = JsonConvert.DeserializeObject(data);

                    AppSettings.PicOfTheDayUrl = json.urls.regular;

                    AppSettings.PicOfTheDayLink = json.user.links.html;

                    AppSettings.PicOfTheDayUser = json.user.name;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);

                Logger.Warn("No picture of the day could be retrieved");
            }
        }

        public static void DownloadVideo(string id)
        {
            if (!AppSettings.DownloadReady)
            {
                Logger.Info("Tools for downloading are not ready yet");

                _ = StatusBoard.PutStatus("downloadResult", id, "false");

                return;
            }

            string path = Directory.GetCurrentDirectory() + @"\youtube-dl.exe";

            int height = 2160, fps = 60;

            switch (AppSettings.PreferredQuality)
            {
                case AppSettings.DownloadQuality.H2160F60:
                    {
                        break;
                    }
                case AppSettings.DownloadQuality.H2160F30:
                    {
                        fps = 30;
                        break;
                    }
                case AppSettings.DownloadQuality.H1440F60:
                    {
                        height = 1440;
                        break;
                    }
                case AppSettings.DownloadQuality.H1440F30:
                    {
                        height = 1440;
                        fps = 30;
                        break;
                    }
                case AppSettings.DownloadQuality.H1080F60:
                    {
                        height = 1080;
                        fps = 60;
                        break;
                    }
                case AppSettings.DownloadQuality.H1080F30:
                    {
                        height = 1080;
                        fps = 30;
                        break;
                    }
                case AppSettings.DownloadQuality.H720F60:
                    {
                        height = 720;
                        fps = 60;
                        break;
                    }
                case AppSettings.DownloadQuality.H720F30:
                    {
                        height = 720;
                        fps = 30;
                        break;
                    }
                default:
                    {
                        Logger.Warn("PreferredQuality is of unexpected value");
                        AppSettings.PreferredQuality = AppSettings.DownloadQuality.H1080F60;
                        height = 1080;
                        fps = 60;
                        break;
                    }
            }

            Process dl = new Process();

            dl.StartInfo.FileName = path;

            const char quote = '\u0022';

            dl.StartInfo.Arguments = $@"-f " + quote + $@"bestvideo[height<={height}][fps<={fps}][ext=webm]+bestaudio[ext=webm]/bestvideo[height<={height}][fps<={fps}][ext=mp4]+bestaudio[ext=m4a]/webm/mp4" + quote + $@" -o wwwroot/Videos/%(channel_id)s&%(uploader)s/%(id)s&%(title)s --restrict-filenames --write-all-thumbnails https://www.youtube.com/watch?v={id}";

            dl.StartInfo.UseShellExecute = false;

            dl.StartInfo.RedirectStandardOutput = true;

            dl.StartInfo.RedirectStandardInput = true;

            dl.StartInfo.CreateNoWindow = true;

            try
            {
                dl.Start();
            }
            catch (Exception m)
            {
                Logger.Info("Couldn't start Download");

                Logger.Info("Make sure youtube-dl.exe is found in the main dir");

                Logger.Error(m.Message);

                _ = StatusBoard.PutStatus("downloadResult", id, "false");
            }

            int highestPercent = 0;

            bool videoDone = false;

            long lastUpdate = DateTime.Now.Ticks - 2000000;

            while (!dl.StandardOutput.EndOfStream)
            {
                string text = dl.StandardOutput.ReadLine();

                if (!videoDone && (DateTime.Now.Ticks - lastUpdate >= 2000000) && text.Contains("download") && text.Contains("%"))
                {
                    try
                    {
                        lastUpdate = DateTime.Now.Ticks;

                        string progress = text.Substring(12, 2);

                        int progressNumber = Int32.Parse(progress);

                        if (progressNumber < highestPercent)
                        {
                            videoDone = true;

                            _ = StatusBoard.PutStatus("downloadProgress", id, "100");
                        }
                        else
                        {
                            highestPercent = progressNumber;

                            _ = StatusBoard.PutStatus("downloadProgress", id, progress);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Warn("Failed sending download progress message");

                        Logger.Error(e.Message);
                    }
                }
            }

            LocalCollection.CollectAllDownloadedVideos();

            _ = StatusBoard.PutStatus("downloadResult", id, "true");
        }
    }
}
