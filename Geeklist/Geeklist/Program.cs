using BGG;
using System.Collections.Generic;
using System.IO;
using static System.Console;

namespace Geeklist
{
    interface IState
    {
        string Delimiter { get; }
        string Collection { get; set; }
        string IgnorePath { get; }
        List<IGame> Games { get; }
        SpecialQuery Query { get; set; }
    }
    class State : IState
    {
        public string Delimiter => $"{Collection ?? ">>>"} ";
        public string IgnorePath => Path.Combine(Directory.GetCurrentDirectory(), "ignore.xml");
        public string Collection { get; set; }
        public List<IGame> Games { get; } = new List<IGame>();
        public SpecialQuery Query { get; set; } = new SpecialQuery();
    }
    class Program
    {
        static void Main()
        {
            IState state = new State();
            while (true)
            {
                Write(state.Delimiter);
                string line = ReadLine();
                ICommand cmd = CommandRegister.MakeCommand(line);
                cmd.Execute(state);
            }
        }
    }
}
