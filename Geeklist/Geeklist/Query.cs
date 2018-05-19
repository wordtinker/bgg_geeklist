using BGG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Geeklist
{
    [Serializable]
    class SpecialQuery : Query
    {
        // Indexer method for setting properties in runtime
        public object this[string propertyName]
        {
            get => GetType().GetProperty(propertyName).GetValue(this, null);
            set
            {
                // Deal with reflexed properties
                PropertyInfo propInfo = GetType().GetProperty(propertyName);
                Type valType = propInfo.PropertyType;
                if (valType == typeof(int?))
                {
                    propInfo.SetValue(this, int.Parse((string)value), null);
                }
                else if (valType == typeof(double?))
                {
                    propInfo.SetValue(this, double.Parse((string)value), null);
                }
                else if (valType == typeof(bool))
                {
                    propInfo.SetValue(this, bool.Parse((string)value), null);
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
                Type propType = prop.PropertyType;
                if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    foreach (var kvp in (Dictionary<string, CategoryDescriptor>)prop.GetValue(this))
                    {
                        yield return ($"{prop.Name}.{kvp.Key.ToString()}", kvp.Value.On);
                    }
                }
                else
                {
                    yield return (prop.Name, this[prop.Name]);
                }
            }
        }
    }
}
