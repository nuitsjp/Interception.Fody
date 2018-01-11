using System;
using System.Collections.Generic;
using System.Reflection;

namespace Weaving
{
    public interface IEventTracker
    {
        void TrackEvent(Type type, PropertyInfo propertyInfo, object value);
    }
}