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
        private readonly EventTrackerManagerImpl _eventTrackerManager;
        //public bool BoolProperty { get; set; }
        public ReactiveProperty<int> IntProperty { get; private set; } = new ReactiveProperty<int>();
        public ReactiveProperty<int> ReadOnlyIntProperty { get; } = new ReactiveProperty<int>();
        public ReadOnlyReactiveProperty<int> ReadOnlyReactiveProperty { get; set; } = new ReactiveProperty<int>().ToReadOnlyReactiveProperty();
        public OriginalReactiveCommand NavigateCommand { get; } = new OriginalReactiveCommand();
        public OriginalReactiveCommand ValiableNavigateCommand { get; private set; } = new OriginalReactiveCommand();
        public AsyncReactiveCommand<string> NavigateAsyncCommand { get; set; } = new AsyncReactiveCommand<string>();
        public MainPageViewModel()
        {
            _eventTrackerManager = new EventTrackerManagerImpl(this);
            _eventTrackerManager.SetEventTracker(ReadOnlyIntProperty);
        }

        public class OriginalReactiveCommand : ReactiveCommand
        {
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
