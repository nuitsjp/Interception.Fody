using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Weaving;

namespace WeaveTarget
{
    public class AssemblyEventTracker : IEventTracker
    {
        public void TrackEvent(Type type, PropertyInfo propertyInfo, object value)
        {
            Console.WriteLine($"AssemblyEventTracker type:{type.Name} property:{propertyInfo.Name} value:{value}");
        }
    }
}
