using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace SubBox.Models
{
    public class Downloader
    {
        public static void DownloadFiles()
        {
            if (!File.Exists("youtube-dl.exe"))
            {
                Logger.Info("youtube-dl.exe not found");

                Logger.Info("Downloading youtube-dl.exe...");

                var client = new WebClient();

                int Progress = 0;

                client.DownloadProgressChanged += (sender, e) => {
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

                client.DownloadFileAsync(new Uri("https://youtube-dl.org/downloads/latest/youtube-dl.exe"), "youtube-dl.exe");
            }

            if (!File.Exists("ffmpeg.exe"))
            {
                Logger.Info("ffmpeg.exe not found");

                Logger.Info("Downloading ffmpeg.exe...");

                var client = new WebClient();

                int Progress = 0;

                client.DownloadProgressChanged += (sender, e) => {
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

            AppSettings.DownloadReady = true;
        }

        public static void DownloadVideo(Video v)
        {
            if (!AppSettings.DownloadReady)
            {
                Logger.Info("Tools for downloading are not ready yet");
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

            var dl = new Process();

            dl.StartInfo.FileName = path;

            dl.StartInfo.Arguments = $@"-f bestvideo[height<={height}][fps<={fps}]+bestaudio/best[height<={height}][fps<={fps}] -o Videos/{v.ChannelId}_%(uploader)s/{v.Id}_%(title)s --restrict-filenames https://www.youtube.com/watch?v={v.Id}";

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
            }
            
            while (!dl.StandardOutput.EndOfStream)
            {
                Logger.Info(dl.StandardOutput.ReadLine());
            }

            LocalCollection.AddLocalVideo(v, v.Id);
        }
    }
}
