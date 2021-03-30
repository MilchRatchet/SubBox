using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SubBox.Data;
using SubBox.Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

namespace SubBox
{
    public class Startup
    {
        public static string BuildVersion;

        //Found at: https://www.meziantou.net/getting-the-date-of-build-of-a-dotnet-assembly-at-runtime.htm
        private static DateTime GetBuildDate(Assembly assembly)
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value.Substring(index + BuildVersionMetadataPrefix.Length);
                    if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddDbContext<AppDbContext>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(AppContext.BaseDirectory + "SubBox.dll");

            BuildVersion = fvi.ProductVersion.Split('+')[0];

            DateTime BuildTime = GetBuildDate(Assembly.GetExecutingAssembly());

            Logger.Info("SubBox Build v"+ BuildVersion + " - " + BuildTime.Day + "." + BuildTime.Month + "." + BuildTime.Year);

            //load settings
            try
            {
                using (StreamReader file = File.OpenText(@"settings.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();

                    serializer.Deserialize(file, typeof(AppSettings));
                }
            }
            catch (Exception e)
            {
                Logger.Warn("AppSettings could not be applied, reverting to default");

                Logger.Error(e.Message);

                AppSettings.DeletionTimeFrame = 30;

                AppSettings.RetrievalTimeFrame = 3;

                AppSettings.NewChannelTimeFrame = 30;

                AppSettings.PlaylistPlaybackSize = 50;

                AppSettings.Color = "DB4437";

                AppSettings.Theme = "ThemeSubBoxLight";

                AppSettings.ConfirmWindow = true;

                AppSettings.NewVersionNotification = true;

                AppSettings.DownloadFinishedNotification = true;

                AppSettings.VideosDeletedNotification = true;

                AppSettings.ChannelAddedNotification = true;

                AppSettings.ChannelsPerPage = 10;

                AppSettings.SmartListLoading = false;

                AppSettings.HighlightNewVideos = true;

                AppSettings.LastRefresh = BuildTime;

                AppSettings.FirstStart = true;

                AppSettings.AutoStart = true;

                AppSettings.DevMode = false;

                AppSettings.UpdateYDL = true;

                AppSettings.PreferredQuality = AppSettings.DownloadQuality.H1080F60;

                AppSettings.Save();
            }

            AppSettings.GCMode = true;

            AppSettings.EnsureDisplayPlaylists();

            AppSettings.SetFirstUse();

            AppSettings.DownloadReady = false;

            Downloader.DownloadFiles();

            if (AppSettings.LastRefresh.DayOfYear != DateTime.Now.DayOfYear || AppSettings.PicOfTheDayLink == "")
            {
                Downloader.GetPictureOfTheDay();
            }

            if (AppSettings.UpdateYDL && AppSettings.DownloadReady)
            {
                string path = Directory.GetCurrentDirectory() + @"\youtube-dl.exe";

                Process ydl = new Process();

                ydl.StartInfo.FileName = path;

                ydl.StartInfo.Arguments = $@"-U";

                ydl.StartInfo.UseShellExecute = false;

                ydl.StartInfo.CreateNoWindow = true;

                try
                {
                    Logger.Info("Checking for youtube-dl updates");

                    ydl.Start();

                    ydl.WaitForExit();
                }
                catch (Exception m)
                {
                    Logger.Info("Couldn't update youtube-dl");

                    Logger.Info("Make sure youtube-dl.exe is found in the main dir");

                    Logger.Error(m.Message);
                }
            }

            using (AppDbContext context = new AppDbContext())
            {
                context.Database.EnsureCreated();
            }

            new Thread(() =>
            {
                LocalCollection.CollectAllDownloadedVideos();
            }).Start();

            new Thread(() =>
            {
                //Update Channel Pictures if this is the first start since the release of version 1.8.0
                if (AppSettings.LastRefresh.Ticks - 637319232000000000L < 0)
                {
                    Logger.Info("Updating Channel Pictures");

                    Downloader.SyncChannelPictures();
                } 
                else
                {
                    Random ran = new Random();

                    if (ran.Next(0, 30) == 0)
                    {
                        Logger.Info("Updating Channel Pictures");

                        Downloader.SyncChannelPictures();
                    }  
                }

                DataRetriever Fetcher = new DataRetriever();

                Fetcher.UpdateVideoList();

                if (AppSettings.AutoStart)
                {
                    Logger.Warn("AutoStart active, opening browser");

                    try
                    {
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {"http://localhost:2828/".Replace("&", "^&")}"));
                    }
                    catch(Exception e)
                    {
                        Logger.Error("Failed starting browser");

                        Logger.Error(e.Message);
                    }   
                }
            }).Start();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseFileServer();

            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });

            new Thread(() =>
            {
                ConsoleHandler.HandleConsoleInput();
            }).Start();
        }
    }
}
