using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static IHostBuilder CreateHostBuilder(string[] args, Action<HostBuilderContext, IServiceCollection> configureServices, Assembly userSecretsAssembly = null)
        {
            //determine the calling assembly for the user secrets
            userSecretsAssembly ??= Assembly.GetCallingAssembly();

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
                    config.AddUserSecrets(userSecretsAssembly, optional: true);

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

        public static Task RunMainMenu(string[] args, Func<string[], IHostBuilder> createHostBuilder, string title = null, bool loop = true, Assembly menuAssembly = null)
        {
            // attempting to use Assembly.GetCallingAssembly() from within an async method doesn't work (always returns CoreLib), so that reference must exist here
            return RunMainMenu(args, createHostBuilder, menuAssembly ?? Assembly.GetCallingAssembly(), title, loop);
        }

        private static async Task RunMainMenu(string[] args, Func<string[], IHostBuilder> createHostBuilder, Assembly assembly, string title = null, bool loop = true)
        {
            if (title != null)
                Console.WriteLine(title + Environment.NewLine);

            // scan for main menu options
            List<(Type Type, MainMenuAttribute Info, Func<IServiceProvider, Task> Action)> options = assembly
                // get all classes that are not abstract
                .GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract)
                // which are marked [MainMenu]
                .Select(x => (x, x.GetCustomAttribute<MainMenuAttribute>()))
                .Where(x => x.Item2 != null)
                // and implement IMenuOption or have a public RunAsync() method
                .Select(x => (x.x, x.Item2, CreateFunc(x.x)))
                .Where(x => x.Item3 != null)
                // sort the list
                .OrderBy(x => x.Item2.SortOrder)
                .ThenBy(x => x.Item2.Name)
                .ToList();

            if (options.Count == 0)
                throw new Exception("No classes found marked with the [MainMenu] attribute and that implement IMenuOption");

            // create the host builder (see above)
            var hostBuilder = createHostBuilder(args);

            // create service collection for database access via dependency injection
            // also have the main app be a service, similar to a controller in asp.net core
            // then the main app can request services in the constructor
            IServiceProvider rootServiceProvider;
            using (var host = hostBuilder.Build()) {
                rootServiceProvider = host.Services;

                while (loop) {
                    // display the main menu
                    Console.WriteLine("Main menu:");
                    for (int i = 0; i < options.Count; i++) {
                        Console.WriteLine($"  {i + 1}: {options[i].Info.Name}");
                    }

                    // ask for a selection
                    Console.Write("Please select: ");
                    var str = Console.ReadLine();

                    // attempt to run the selection
                    if (int.TryParse(str, out int num) && num >= 1 && num <= options.Count) {
                        Console.WriteLine();
                        await RunSelection(options[num - 1].Action);
                    }

                    // quit when just pressing enter
                    if (str == "")
                        return;

                    // if looping, write a blank line
                    if (loop)
                        Console.WriteLine();
                }

                // disposing of the host will dispose of any singleton objects
            }

            // terminate program here

            async Task RunSelection(Func<IServiceProvider, Task> func)
            {
                // create a scope and run the app (synchronously)
                using (var scope = rootServiceProvider.CreateScope()) {
                    var serviceProvider = scope.ServiceProvider;

                    // if looping, catch and display errors
                    if (loop) {
                        try {
                            await func(serviceProvider);
                        } catch (Exception e) {
                            Console.WriteLine(e.ToString());
                        }
                    } else {
                        await func(serviceProvider);
                    }

                    // disposing of the scope will dispose of required scoped objects like database contexts
                }
            }

            Func<IServiceProvider, Task> CreateFunc(Type t)
            {
                if (t is IMenuOption) {
                    return serviceProvider => {
                        // if T is not registered with the service provider, create an instance of it for us to use here
                        var obj = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, t);
                        return ((IMenuOption)obj).RunAsync();
                    };
                } else {
                    var method = t.GetMethod("RunAsync");
                    if (method != null && method.ReturnType == typeof(Task) && method.GetParameters().Length == 0) {
                        return serviceProvider => {
                            // if T is not registered with the service provider, create an instance of it for us to use here
                            var obj = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, t);
                            return (Task)method.Invoke(obj, null);
                        };
                    }
                    method = t.GetMethod("Run");
                    if (method != null && method.ReturnType == null && method.GetParameters().Length == 0) {
                        return serviceProvider => {
                            // if T is not registered with the service provider, create an instance of it for us to use here
                            var obj = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, t);
                            method.Invoke(obj, null);
                            return Task.CompletedTask;
                        };
                    }
                }
                return null;
            }
        }

    }
}
