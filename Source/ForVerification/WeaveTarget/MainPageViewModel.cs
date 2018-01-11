using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using Weaving;

namespace WeaveTarget
{
    [TrackEvent(typeof(EventTracker))]
    public class MainPageViewModel
    {
        public bool BoolProperty { get; set; }
        public ReactiveProperty<int> IntProperty { get; } = new ReactiveProperty<int>();

        public MainPageViewModel()
        {
            //IntProperty.Skip(1).Subscribe();
            //TrackEventInitializer.Init(this);
        }
    }

    public class EventTracker : IEventTracker
    {
        public void TrackEvent(Type type, PropertyInfo propertyInfo, object value)
        {
            Console.WriteLine($"type:{type.Name} property:{propertyInfo.Name} value:{value}");
        }
    }
}
