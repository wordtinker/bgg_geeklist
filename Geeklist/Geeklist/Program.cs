using static System.Console;

namespace Geeklist
{
    interface IState
    {
        string Delimiter { get; }
        string Collection { get; set; }
    }
    class State : IState
    {
        public string Delimiter => $"{Collection ?? ">>>"} ";
        public string Collection { get; set; }
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
