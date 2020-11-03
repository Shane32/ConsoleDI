using System;
using System.Collections.Generic;
using System.Text;

namespace Shane32.ConsoleDI
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MainMenuAttribute : Attribute
    {
        public string Name { get; set; }
        public MainMenuAttribute(string name)
        {
            Name = name;
        }

        public int SortOrder { get; set; }
    }
}
