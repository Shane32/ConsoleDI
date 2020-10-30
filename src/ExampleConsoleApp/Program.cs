using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shane32.ConsoleDI;

namespace ExampleConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
            => await ConsoleHost.RunAsync<App>(args, CreateHostBuilder, app => app.RunAsync());

        // this function is necessary for Entity Framework Core tools to perform migrations, etc
        // do not change signature!!
        public static IHostBuilder CreateHostBuilder(string[] args)
            => ConsoleHost.CreateHostBuilder(args, ConfigureServices);

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            // register your Entity Framework data contexts here

            services.AddScoped<Random>(_ => {
                var seedStr = context.Configuration["Config:Seed"];
                if (int.TryParse(seedStr, out int seed)) {
                    return new Random(seed);
                }
                return new Random();
            });
        }

    }
}
