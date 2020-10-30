using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shane32.ConsoleDI
{
    public static class ConsoleHost
    {
        public static async Task RunAsync<T>(string[] args, Func<string[], IHostBuilder> createHostBuilder, Func<T, Task> action)
        {
            // create the host builder (see below)
            var hostBuilder = createHostBuilder(args);

            // create service collection for database access via dependency injection
            // also have the main app be a service, similar to a controller in asp.net core
            // then the main app can request services in the constructor
            using (var host = hostBuilder.Build()) {
                var rootServiceProvider = host.Services;

                // create a scope and run the app (synchronously)
                using (var scope = rootServiceProvider.CreateScope()) {
                    var serviceProvider = scope.ServiceProvider;
                    // if T is not registered with the service provider, create an instance of it for us to use here
                    var app = ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);
                    await action(app);

                    // disposing of the scope will dispose of required objects like database contexts
                }

                // disposing of the host will dispose of any singleton objects
            }

            // terminate program here
        }

        public static void Run<T>(string[] args, Func<string[], IHostBuilder> createHostBuilder, Action<T> action)
        {
            // create the host builder (see below)
            var hostBuilder = createHostBuilder(args);

            // create service collection for database access via dependency injection
            // also have the main app be a service, similar to a controller in asp.net core
            // then the main app can request services in the constructor
            using (var host = hostBuilder.Build()) {
                var rootServiceProvider = host.Services;

                // create a scope and run the app (synchronously)
                using (var scope = rootServiceProvider.CreateScope()) {
                    var serviceProvider = scope.ServiceProvider;
                    var app = serviceProvider.GetRequiredService<T>();
                    action(app);

                    // disposing of the scope will dispose of required objects like database contexts
                }

                // disposing of the host will dispose of any singleton objects
            }

            // terminate program here
        }

        public static IHostBuilder CreateHostBuilder(string[] args, Action<HostBuilderContext, IServiceCollection> configureServices)
        {
            //determine the calling assembly for the user secrets
            var assembly = Assembly.GetCallingAssembly();

            var builder = new HostBuilder()
                // set location to scan for appsettings.json file
                .UseContentRoot(Directory.GetCurrentDirectory())

                // read host configuration environment variables
                .ConfigureHostConfiguration(config => {
                    config.AddEnvironmentVariables(prefix: "DOTNET_");
                    if (args != null)
                        config.AddCommandLine(args);
                })

                // read application configuration settings
                .ConfigureAppConfiguration((hostingContext, config) => {
                    var env = hostingContext.HostingEnvironment;

                    // read appsettings.json first (lowest priority)
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                    // read appsettings.Debug.json or similar
                    config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                    // read user secrets configured for the appropriate assembly
                    // note: cannot get the calling assembly here, because this is a lambda function, so the calling assembly is ConsoleDI
                    // note: cannot get the entry assembly here, because with EF migrations it would be EF, not the correct assembly
                    config.AddUserSecrets(assembly, optional: true);

                    // read environment variables
                    config.AddEnvironmentVariables();

                    // read command line arguments (highest priority)
                    if (args != null) {
                        config.AddCommandLine(args);
                    }
                })

                // configure the service provider options
                .UseDefaultServiceProvider((context, options) => {
                    options.ValidateScopes = true;
                    options.ValidateOnBuild = true;
                })

                // configure the services defined by the application
                .ConfigureServices(configureServices);

            // return the configured HostBuilder
            return builder;
        }
    }
}
