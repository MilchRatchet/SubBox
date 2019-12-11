using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            string version = fvi.ProductVersion.Split('+')[0];

            DateTime BuildTime = GetBuildDate(Assembly.GetExecutingAssembly());

            Logger.Info("SubBox Build v"+ version +" - " + BuildTime.Day + "." + BuildTime.Month + "." + BuildTime.Year);

            AppSettings.DownloadReady = false;

            Downloader.DownloadFiles();

            //load settings
            try
            {
                string[] options = File.ReadAllLines("settings.txt");

                AppSettings.RetrievalTimeFrame = int.Parse(options[0]);

                AppSettings.NewChannelTimeFrame = int.Parse(options[1]);

                AppSettings.DeletionTimeFrame = int.Parse(options[2]);

                AppSettings.PlaylistPlaybackSize = int.Parse(options[3]);

                AppSettings.Color = options[4];

                if (options[5]=="True")
                {
                    AppSettings.NightMode = true;
                } else
                {
                    AppSettings.NightMode = false;
                }

                AppSettings.LastRefresh = DateTime.ParseExact(options[6], "O", CultureInfo.InvariantCulture);

                if (options[7] == "True")
                {
                    AppSettings.FirstStart = true;
                }
                else
                {
                    AppSettings.FirstStart = false;
                }

                if (options[8] == "True")
                {
                    AppSettings.AutoStart = true;
                }
                else
                {
                    AppSettings.AutoStart = false;
                }

                if (options[9] == "True")
                {
                    AppSettings.DevMode = true;
                }
                else
                {
                    AppSettings.DevMode = false;
                }

                AppSettings.PreferredQuality = (AppSettings.DownloadQuality) Enum.Parse(typeof(AppSettings.DownloadQuality), options[10]);
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

                AppSettings.NightMode = false;

                AppSettings.LastRefresh = BuildTime;

                AppSettings.FirstStart = true;

                AppSettings.AutoStart = true;

                AppSettings.DevMode = false;

                AppSettings.PreferredQuality = AppSettings.DownloadQuality.H1080F60;

                AppSettings.Save();
            }

            AppSettings.GCMode = true;

            using (AppDbContext context = new AppDbContext())
            {
                context.Database.EnsureCreated();

                context.Database.ExecuteSqlCommand("CREATE TABLE IF NOT EXISTS Tags (Name TEXT PRIMARY KEY, Filter TEXT);");

            }

            new Thread(() =>
            {
                LocalCollection.CollectAllDownloadedVideos();
            }).Start();

            new Thread(() =>
            {
                DataRetriever Fetcher = new DataRetriever();

                Fetcher.UpdateVideoList();

                if (AppSettings.AutoStart)
                {
                    Logger.Warn("AutoStart active, opening browser");

                    try
                    {
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {"http://localhost:5000/".Replace("&", "^&")}"));
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
