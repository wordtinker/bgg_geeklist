using BGG;
using System.Collections.Generic;

namespace Geeklist
{
    class GeekItemComparer : IEqualityComparer<IGeekItem>
    {
        public bool Equals(IGeekItem x, IGeekItem y)
        {
            //Check whether any of the compared objects is null.
            if (x is null || y is null || x.Game is null || y.Game is null)
                return false;
            //Check whether the objects' properties are equal.
            return x.Game.Id == y.Game.Id;
        }

        public int GetHashCode(IGeekItem geekItem)
        {
            //Check whether the object is null
            if (geekItem is null || geekItem.Game is null)
                return 0;

            return geekItem.Game.Id.GetHashCode();
        }
    }
}
