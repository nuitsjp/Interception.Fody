using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Reactive.Bindings;

namespace Weaving
{
    public abstract class EventTrackerManagerBase
    {
        private static bool _isInitializedAssembly;

        private static IEventTracker _assemblyEventTracker;

        private IEventTracker AssemblyEventTracker
        {
            get
            {
                if (!_isInitializedAssembly)
                {
                    _assemblyEventTracker =
                        CreateEventTracker(GetType().Assembly.GetCustomAttribute<TrackEventAttribute>());
                    _isInitializedAssembly = true;
                }
                return _assemblyEventTracker;
            }
        }
        private IEventTracker TypeEventTracker { get; }


        protected EventTrackerManagerBase(object instance)
        {
            TypeEventTracker = CreateEventTracker(instance.GetType().GetCustomAttribute<TrackEventAttribute>());

            Console.WriteLine($"AssemblyEventTracker:{AssemblyEventTracker}");
            Console.WriteLine($"TypeEventTracker:{TypeEventTracker}");
        }

        public void SetEventTracker<T>(IReactiveProperty<T> reactiveProperty)
        {
            
        }

        private static IEventTracker CreateEventTracker(TrackEventAttribute trackEventAttribute)
        {
            if (trackEventAttribute != null)
                return Activator.CreateInstance(trackEventAttribute.EventTrackerType) as IEventTracker;

            return null;
        }
    }
}
