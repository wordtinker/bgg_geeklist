using BGG;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Geeklist
{
    static class XMLConverter
    {
        public static XDocument ToXML(IEnumerable<IGame> games)
        {
            var serialized =
                games.Select(g
                => new XElement("listItem",
                    new XAttribute("gameId", g.Id),
                    new XAttribute("gameName", g.Name),
                    new XAttribute("uri", g.ToString())
                ));
            return new XDocument(new XElement("Root", serialized));
        }
        public static XDocument ToXML(IEnumerable<IGeekItem> games)
        {
            return ToXML(games.Select(gi => gi.Game));
        }
        public static List<IGeekItem> FromXML(XDocument xdoc)
        {
            var deserialized
                = from elem in xdoc.Descendants("listItem")
                  select new GeekItem
                  {
                      Game = new Game
                      {
                          Id = int.Parse(elem.Attribute("gameId").Value),
                          Name = elem.Attribute("gameName").Value
                      }
                  };
            return deserialized.ToList<IGeekItem>();
        }
    }
}
