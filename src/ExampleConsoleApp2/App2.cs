using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shane32.ConsoleDI;

namespace ExampleConsoleApp2
{
    [MainMenu("App2")]
    class App2 : IMenuOption
    {
        public async Task RunAsync()
        {
            Console.WriteLine("This is an alternate program");
        }
    }
}
