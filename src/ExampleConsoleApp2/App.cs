using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shane32.ConsoleDI;

namespace ExampleConsoleApp2
{
    [MainMenu("App1")]
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
