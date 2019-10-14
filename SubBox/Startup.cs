using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubBox.Data;
using SubBox.Models;
using System;
using System.IO;

namespace SubBox
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddDbContext<AppDbContext>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
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
            }
            catch (Exception)
            {
                AppSettings.DeletionTimeFrame = 7;
                AppSettings.RetrievalTimeFrame = 7;
                AppSettings.NewChannelTimeFrame = 1;
                AppSettings.PlaylistPlaybackSize = 50;
                AppSettings.Color = "ff0000";
                AppSettings.NightMode = false;
                AppSettings.Save();
            }


            using (AppDbContext context = new AppDbContext())
            {
                context.Database.EnsureCreated();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseFileServer();

            app.UseMvc();
        }
    }
}
