using Hangfire.MemoryStorage;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using NLog.Web;

namespace FHIRcastSandbox {
    public class Program {
        public static void Main(string[] args) {
            BuildWebHost(args).Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseNLog();
        }

        public static IWebHost BuildWebHost(string[] args) => CreateWebHostBuilder(args).Build();
    }
}
