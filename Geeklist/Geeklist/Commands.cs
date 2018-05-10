using static System.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BGG;
using System.Xml.Linq;

namespace Geeklist
{
    interface ICommand
    {
        void Execute(IState state);
    }
    class IgnorePos : ICommand
    {
        private int position;
        public IgnorePos(int position)
        {
            this.position = position;
        }
        public void Execute(IState state)
        {
            if (position > 0 && position <= state.Games.Count)
            {
                IGame toIgnore = state.Games[position - 1];
                int gameId = int.Parse(toIgnore.Id);
                ICommand ignore = new Ignore(gameId);
                ignore.Execute(state);
            }
            else
            {
                WriteLine("Index is out of range.");
            }
        }
    }
    class Ignore : ICommand
    {
        private int gameId;
        public Ignore(int gameId)
        {
            this.gameId = gameId;
        }
        public void Execute(IState state)
        {
            IGeekItem newItem = new GeekItem
            {
                Game = new Game
                {
                    Id = gameId.ToString(),
                    Name = string.Empty
                }
            };
            List<IGeekItem> gameList;
            string path = state.IgnorePath;
            gameList = File.Exists(path) ? XMLConverter.FromXML(XDocument.Load(path)) : new List<IGeekItem>();
            gameList.Add(newItem);
            XDocument xml = XMLConverter.ToXML(gameList);
            xml.Save(path);
            
        }
    }
    class Stats : ICommand
    {
        private int from;
        private int to;
        public Stats(int from = 1, int to = 20)
        {
            this.from = from - 1;
            this.to = to;
        }
        public void Execute(IState state)
        {
            if (state.Collection != null)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), state.Collection);
                var games =
                    Directory.GetFiles(path)
                    .Select(f => XDocument.Load(f))
                    .SelectMany(xdoc => XMLConverter.FromXML(xdoc));

                GeekItemComparer cmp = new GeekItemComparer();
                string ignorePath = state.IgnorePath;
                var ignoredGames = File.Exists(ignorePath) ? XMLConverter.FromXML(XDocument.Load(ignorePath)) : new List<IGeekItem>();
                var notIgnored = games.Where(g => !ignoredGames.Contains(g, cmp));

                var filtered =
                    notIgnored
                    .GroupBy(g => g, cmp)
                    .Select(grp =>
                        new
                        {
                            grp.First().Game,
                            Count = grp.Count()
                        })
                    .OrderByDescending(g => g.Count);
                var result = filtered.Skip(from).Take(to - from);
                state.Games.Clear();
                state.Games.AddRange(result.Select(g => g.Game));
                int i = 1;
                foreach (var item in result)
                {
                    WriteLine($"{i + from}) {item.Game.Id} :: {item.Game.Name} -- {item.Count}");
                    i++;
                }
            }
            else
            {
                WriteLine("Stage collection.");
            }
        }
    }
    //class DeleteList : ICommand
    //{
    //    // TODO Later
    //}

    class GetList : ICommand
    {
        private int listId;
        public GetList(int listId)
        {
            this.listId = listId;
        }
        public void Execute(IState state)
        {
            if (state.Collection != null)
            {
                var api = new BGG.API(new APIConfig());
                List<IGeekItem> result = api.GetGeekListAsync(listId).Result;
                XDocument xml = XMLConverter.ToXML(result);

                string path = Path.Combine(Directory.GetCurrentDirectory(), state.Collection, listId.ToString());
                path = Path.ChangeExtension(path, ".xml");

                xml.Save(path);
            }
            else
            {
                WriteLine("Stage collection.");
            }
        }
    }
    //class ShowCollection : ICommand
    //{
    //    // TODO Later
    //}

    //class DeleteCollection : ICommand
    //{
    //    // TODO Later
    //}

    class SetCollection : ICommand
    {
        private string file;
        public SetCollection(string file)
        {
            this.file = file;
        }
        public void Execute(IState state)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), file);
            if (Directory.Exists(path))
            {
                state.Collection = file;
                state.Games.Clear();
            }
            else
            {
                WriteLine("Illegal collection name.");
            }
        }
    }
    class CreateCollection : ICommand
    {
        private string file;
        public CreateCollection(string file)
        {
            this.file = file;
        }
        public void Execute(IState state)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), file);
            Directory.CreateDirectory(path);
        }
    }
    class ListCollections : ICommand
    {
        public void Execute(IState state)
        {
            string cwd = Directory.GetCurrentDirectory();
            foreach (string dir in Directory.GetDirectories(cwd))
            {
                WriteLine($"-- {dir}");
            }
        }
    }
    class Exit : ICommand
    {
        public void Execute(IState state)
        {
            Environment.Exit(0);
        }
    }
    class Help : ICommand
    {
        public void Execute(IState state)
        {
            WriteLine("`position` :: Ignore game by position in the list.");
            WriteLine("ignore `id` :: Ignore given game `id`.");
            WriteLine("stats `depth` :: Show stats for selected collection");
            WriteLine("stats :: Show stats for selected collection");
            WriteLine("get `id` :: Get geelist from BGG with a given `id`.");
            WriteLine("stage `name` :: Choose working collection with a given `name`.");
            WriteLine("create `name` :: Create new collection with a given `name`.");
            WriteLine("ls :: Show list of existing collections.");
            WriteLine("exit :: Terminate application.");
        }
    }
    class Void : ICommand
    {
        public void Execute(IState state)
        {
            WriteLine("Illegal command. See 'help'.");
        }
    }
    static class CommandRegister
    {
        public static ICommand MakeCommand(string line)
        {
            string[] parts = line.Split(' ');
            string name = parts[0];
            string[] args = parts.Skip(1).ToArray();
            switch (name)
            {
                case "exit":
                    return new Exit();

                case "help":
                    return new Help();

                case "ls":
                    return new ListCollections();

                case "create" when args.Length > 0:
                    return new CreateCollection(args[0]);

                case "stage" when args.Length > 0 && args[0].Trim().Length > 0:
                    return new SetCollection(args[0]);

                case "get" when args.Length > 0 && int.TryParse(args[0], out int listId):
                    return new GetList(listId);

                case "stats" when args.Length > 1 &&
                    int.TryParse(args[0], out int from) && from >= 1 &&
                    int.TryParse(args[1], out int to) && to > from:
                    return new Stats(from, to);

                case "stats" when args.Length > 0 &&
                    int.TryParse(args[0], out int to) && to >= 1:
                    return new Stats(1, to);

                case "stats":
                    return new Stats();

                case "ignore" when args.Length > 0 && int.TryParse(args[0], out int gameId):
                    return new Ignore(gameId);
                
                case string val when int.TryParse(val, out int gamePos) && gamePos > 0:
                    return new IgnorePos(gamePos);

                default:
                    return new Void();
            }
        }
    }
}
