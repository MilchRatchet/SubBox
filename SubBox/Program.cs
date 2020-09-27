using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace SubBox
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseUrls("http://localhost:2828", "http://*:2828")
                .UseStartup<Startup>();
    }
}
