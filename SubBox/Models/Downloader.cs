﻿using System;
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
                Console.WriteLine("youtube-dl.exe not found");

                Console.WriteLine("Downloading youtube-dl.exe...");

                var client = new WebClient();

                int Progress = 10;

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
                Console.WriteLine("ffmpeg.exe not found");

                Console.WriteLine("Downloading ffmpeg.exe...");

                var client = new WebClient();

                int Progress = 10;

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
                    catch (Exception)
                    {

                    }
                };

                client.DownloadFileAsync(new Uri("https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-4.2.1-win64-static.zip"), "ffmpeg.zip");
            }
        }

        public static void DownloadVideo(Video v)
        {
            string path = Directory.GetCurrentDirectory() + @"\youtube-dl.exe";

            var dl = new Process();

            dl.StartInfo.FileName = path;

            dl.StartInfo.Arguments = $@"-f bestvideo[height<=1440]+bestaudio/best[height<=1440] -o Videos/%(uploader)s/%(title)s.mp4 https://www.youtube.com/watch?v={v.Id}";

            dl.StartInfo.UseShellExecute = false;

            dl.StartInfo.RedirectStandardOutput = true;

            dl.StartInfo.RedirectStandardInput = true;

            dl.StartInfo.CreateNoWindow = true;

            dl.Start();

            while (!dl.StandardOutput.EndOfStream)
            {
                Console.WriteLine(dl.StandardOutput.ReadLine());
            }
        }
    }
}
