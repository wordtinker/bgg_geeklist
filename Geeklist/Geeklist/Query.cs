using BGG;
using System.Collections.Generic;

namespace Geeklist
{
    class SpecialQuery : Query
    {
        // Indexer method for setting properties in runtime
        public object this[string propertyName]
        {
            get => GetType().GetProperty(propertyName).GetValue(this, null);
            set => GetType().GetProperty(propertyName).SetValue(this, value, null);
        }
        public IEnumerable<(string Name, object Value)> PropAndValues()
        {
            foreach (var prop in GetType().BaseType.GetProperties())
            {
                yield return (prop.Name, this[prop.Name]);
            }
        }
    }
}
