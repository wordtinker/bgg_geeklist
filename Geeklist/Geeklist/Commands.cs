using static System.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BGG;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Geeklist
{
    internal class GameContainer
    {
        internal IGame Game { get; set; }
        internal int Count { get; set; }
    }
    interface ICommand
    {
        void Execute(IState state);
    }
    class Peek : ICommand
    {
        private int gameId;
        private int limit;
        public Peek(int gameId, int limit=10_000)
        {
            this.gameId = gameId;
            this.limit = limit;
        }
        public void Execute(IState state)
        {
            var collections = from path in (new ListCollections()).MakeList()
                    from gc in (new Stats(1, limit)).MakeList(path, state.IgnorePath)
                    where gc.Game.Id == gameId
                    select new
                    {
                        path,
                        gc.Game,
                        gc.Count
                    };
            foreach (var pack in collections)
            {
                string collection = pack.path.Split(Path.DirectorySeparatorChar).Last();
                WriteLine($"{pack.Game.Id} :: {pack.Game.Name} -- {pack.Count} <==< {collection}");
            }
        }
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
                int gameId = state.Games[position - 1].Id;
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
                    Id = gameId,
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
        internal List<GameContainer> MakeList(string collection, string ignorePath)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), collection);
            var games =
                Directory.GetFiles(path)
                .Select(f => XDocument.Load(f))
                .SelectMany(xdoc => XMLConverter.FromXML(xdoc));

            GeekItemComparer cmp = new GeekItemComparer();
            var ignoredGames = File.Exists(ignorePath) ? XMLConverter.FromXML(XDocument.Load(ignorePath)) : new List<IGeekItem>();
            var notIgnored = games.Where(g => !ignoredGames.Contains(g, cmp));

            var filtered =
                notIgnored
                .GroupBy(g => g, cmp)
                .Select(grp =>
                    new GameContainer
                    {
                        Game = grp.First().Game,
                        Count = grp.Count()
                    })
                .OrderByDescending(g => g.Count);
            var result = filtered.Skip(from).Take(to - from);
            return result.ToList();
        }
        public void Execute(IState state)
        {
            if (state.Collection != null)
            {
                List<GameContainer> res = MakeList(state.Collection, state.IgnorePath);
                state.Games.Clear();
                state.Games.AddRange(res.Select(g => g.Game));
                int i = 1;
                foreach (var item in res)
                {
                    WriteLine($"{i}) {item.Game.Id} :: {item.Game.Name} -- {item.Count}");
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

    class SaveQuery : ICommand
    {
        private string name;

        public SaveQuery(string name)
        {
            this.name = name;
        }
        public void Execute(IState state)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream($"{name}.bin",
                    FileMode.Create,
                    FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, state.Query);
                stream.Close();
            }
            catch (Exception)
            {
                WriteLine("Save error.");
            }
        }
    }

    class LoadQuery : ICommand
    {
        private string name;

        public LoadQuery(string name)
        {
            this.name = name;
        }
        public void Execute(IState state)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream($"{name}.bin",
                    FileMode.Open,
                    FileAccess.Read, FileShare.Read);
                state.Query = (SpecialQuery)formatter.Deserialize(stream);
                stream.Close();
            }
            catch (Exception)
            {
                WriteLine("Load error.");
            }
        }
    }

    class DoQuery : ICommand
    {
        public void Execute(IState state)
        {
            if (state.Collection != null)
            {
                var api = new BGG.API(new APIConfig());
                List<IGame> result = api.GetQueryAsync(state.Query).Result;
                XDocument xml = XMLConverter.ToXML(result);

                string name = $"{DateTime.Now.ToString("yyyy-M-dd--HH-mm")}_query.xml";
                string path = Path.Combine(Directory.GetCurrentDirectory(), state.Collection, name);

                xml.Save(path);
            }
            else
            {
                WriteLine("Stage collection.");
            }
        }
    }

    class ConfigQuery : ICommand
    {
        private string prop;
        private string val;

        public ConfigQuery(string prop, string val)
        {
            this.prop = prop;
            this.val = val;
        }
        public void Execute(IState state)
        {
            try
            {
                state.Query[prop] = val;
            }
            catch (Exception)
            {
                WriteLine("Wrong property name or value.");
            }
        }
    }

    class ConfigCompoundQuery : ICommand
    {
        private string dicName;
        private string prop;
        private string val;

        public ConfigCompoundQuery(string dicName, string prop, string val)
        {
            this.dicName = dicName;
            this.prop = prop;
            this.val = val;
        }
        public void Execute(IState state)
        {
            Dictionary<string, CategoryDescriptor> dic = null;
            if (dicName == "Category")
            {
                dic = state.Query.Categories;
            }
            else if (dicName == "Domain")
            {
                dic = state.Query.Domains;
            }
            else if (dicName == "Mechanic")
            {
                dic = state.Query.Mechanics;
            }
            // else dic is null and fails
            try
            {
                bool? newValue = null;
                if (bool.TryParse(val, out bool x))
                    newValue = x;
                dic[prop].On = newValue;
            }
            catch (Exception)
            {
                WriteLine("Wrong property name or value.");
            }
        }
    }

    class ShowQuery : ICommand
    {
        public void Execute(IState state)
        {
            foreach (var (Name, Value) in state.Query.PropAndValues())
            {
                if (!(Value is null))
                    WriteLine($"  {Name} = {Value}");
            }
        }
    }

    class GetHot : ICommand
    {
        public void Execute(IState state)
        {
            if (state.Collection != null)
            {
                var api = new BGG.API(new APIConfig());
                List<IGame> result = api.GetHotAsync().Result;
                XDocument xml = XMLConverter.ToXML(result);

                string name = $"{DateTime.Now.ToString("yyyy-M-dd--HH-mm")}_hot.xml";
                string path = Path.Combine(Directory.GetCurrentDirectory(), state.Collection, name);

                xml.Save(path);
            }
            else
            {
                WriteLine("Stage collection.");
            }
        }
    }

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
        internal string[] MakeList()
        {
            string cwd = Directory.GetCurrentDirectory();
            return Directory.GetDirectories(cwd);
        }
        public void Execute(IState state)
        {
            foreach (string dir in MakeList())
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
    class QueryHelp : ICommand
    {
        public void Execute(IState state)
        {
            WriteLine("query :: Run advanced searh.");
            WriteLine("qsave `filename` :: Save current query params to file.");
            WriteLine("qload `filename` :: Restore query params from file.");
            WriteLine("qshow :: Display query params.");
            WriteLine("qset `param` `value` :: Set query parameter.");
            WriteLine("qset Category `param` `value` :: Set category parameter. Value is true/false/null");
            WriteLine("List of parameters:");
            foreach (var (Name, Value) in state.Query.PropAndValues())
            {
                WriteLine($"  {Name}");
            }
        }
    }
    class Help : ICommand
    {
        public void Execute(IState state)
        {
            WriteLine("qhelp :: Help for query engine.");
            WriteLine("peek `gameId`:: Search for `gameId` across all the collections");
            WriteLine("`position` :: Ignore game by position in the list.");
            WriteLine("ignore `id` :: Ignore given game `id`.");
            WriteLine("stats `from` `to` :: Show stats for selected collection");
            WriteLine("stats `depth` :: Show stats for selected collection");
            WriteLine("stats :: Show stats for selected collection");
            WriteLine("get hot :: Get hot section from BGG.");
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

                case "qhelp":
                    return new QueryHelp();

                case "help":
                    return new Help();

                case "ls":
                    return new ListCollections();

                case "create" when args.Length > 0:
                    return new CreateCollection(args[0]);

                case "stage" when args.Length > 0 && args[0].Trim().Length > 0:
                    return new SetCollection(args[0]);

                case "get" when args.Length > 0 && args[0] == "hot":
                    return new GetHot();

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

                case "peek" when args.Length > 0 && int.TryParse(args[0], out int gameId):
                    return new Peek(gameId);

                case "ignore" when args.Length > 0 && int.TryParse(args[0], out int gameId):
                    return new Ignore(gameId);

                case "query":
                    return new DoQuery();
                // E.g. qset Category AbstractStrategy true
                case "qset" when args.Length == 3:
                    return new ConfigCompoundQuery(args[0], args[1], args[2]);
                
                    // E.g qset Publisher 14
                case "qset" when args.Length == 2:
                    return new ConfigQuery(args[0], args[1]);

                case "qsave" when args.Length > 0:
                    return new SaveQuery(args[0]);

                case "qload" when args.Length > 0:
                    return new LoadQuery(args[0]);

                case "qshow":
                    return new ShowQuery();

                case string val when int.TryParse(val, out int gamePos) && gamePos > 0:
                    return new IgnorePos(gamePos);

                default:
                    return new Void();
            }
        }
    }
}
