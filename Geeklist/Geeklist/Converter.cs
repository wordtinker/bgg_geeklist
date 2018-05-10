﻿using BGG;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Geeklist
{
    static class XMLConverter
    {
        public static XDocument ToXML(IEnumerable<IGeekItem> games)
        {
            var serialized =
                games.Select(i
                => new XElement("listItem",
                    new XAttribute("gameId", i.Game.Id),
                    new XAttribute("gameName", i.Game.Name)
                ));
            return new XDocument(new XElement("Root", serialized));
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
