# Shane32.ConsoleDI

[![NuGet](https://img.shields.io/nuget/v/Shane32.ConsoleDI.svg)](https://www.nuget.org/packages/Shane32.ConsoleDI)

This project helps create simple console applications that are run via dependency injection.
It particularly helps when creating applications that have connection strings stored in
user secrets, and when attempting Entity Framework Core migrations.

To use the project, you will need some bootstrap code in your `Program.cs` file
which is used to configure the services and provide the hook for EF Core.  Below is a
sample of a program that registers a single scoped service:

```csharp
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
```

Once the `Program.cs` has been configured, you write a standard class that will be activated
via dependency injection with all of the necessary services.  Below is a sample of a program
that displays a random number, using a random number generator obtained via dependency injection:

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ExampleConsoleApp
{
    class App
    {
        private readonly Random _rng;

        public App(Random rng)
        {
            _rng = rng;
        }

        public async Task RunAsync()
        {
            Console.WriteLine($"Generating a random number via Dependency Injection: {_rng.Next(1, 100)}");
        }
    }
}
```

The program will support pulling configuration from the appsettings.json file, the command line, environment
variables, or user secrets.  To use user secrets, you will need to add a reference to `Microsoft.Extensions.Configuration.UserSecrets`.
Then within Visual Studio, simply right-click the project name and click "Manage User Secrets" to edit the
secrets.  Below is a sample appsettings.json or user-secrets file:

```json
{
  "Config": {
    "Seed": 5993
  }
}
```

Running the program without any configured seed will return a random number between 1 and 100.
With the configuration shown above in the user secrets or `appsettings.json` file, it will print the following:

```
Generating a random number via Dependency Injection: 62
```

If you execute the program at a command prompt, you can feed it a specific seed value:

```
>ExampleConsoleApp Config:Seed=5
Generating a random number via Dependency Injection: 34
```

The order of priority is configured as follows (last has highest priority):

1. `appsettings.json`
2. `appsettings.ENV.json` where ENV is the active configuration (e.g. `appsettings.Debug.json`)
3. User secrets
4. Environment variables
5. Command-line arguments
