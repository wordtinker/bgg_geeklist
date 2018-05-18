using BGG;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Geeklist
{
    class SpecialQuery : Query
    {
        // Indexer method for setting properties in runtime
        public object this[string propertyName]
        {
            get => GetType().GetProperty(propertyName).GetValue(this, null);
            set
            {
                PropertyInfo propInfo = GetType().GetProperty(propertyName);
                Type valType = propInfo.PropertyType;
                if (valType == typeof(int?))
                {
                    propInfo.SetValue(this, int.Parse((string)value), null);
                }
                else
                {
                    propInfo.SetValue(this, value, null);
                }
            }
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
