using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Reactive.Bindings;

namespace Weaving
{
    public static class TrackEventInitializer
    {
        public static void Init(object target)
        {
            var trackEventAttribute = target.GetType().GetTypeInfo().GetCustomAttribute<TrackEventAttribute>();
            var tracker = (IEventTracker)Activator.CreateInstance(trackEventAttribute.EventTrackerType);
            foreach (var propertyInfo in target.GetType().GetRuntimeProperties())
            {
                if (propertyInfo.PropertyType.GetTypeInfo().ImplementedInterfaces.Any(x =>
                    x.GetTypeInfo().IsGenericType &&
                    x.GetGenericTypeDefinition() == typeof(IReactiveProperty<>)))
                {
                    var property = propertyInfo.GetValue(target);

                    var skip = typeof(Observable).GetTypeInfo().GetDeclaredMethods("Skip").Single(x => x.GetParameters()[1].ParameterType == typeof(int));
                    var skipGeneric =
                        skip.MakeGenericMethod(propertyInfo.PropertyType.GetTypeInfo().GenericTypeArguments);
                    var observable = skipGeneric.Invoke(null, new[] {property, 1});
                    var subscribe = typeof(ObservableExtensions).GetTypeInfo().GetDeclaredMethods("Subscribe").Single(x => x.GetParameters().Length == 2 && x.GetParameters()[1].ParameterType != typeof(CancellationToken));
                    var subscribeGeneric =
                        subscribe.MakeGenericMethod(propertyInfo.PropertyType.GetTypeInfo().GenericTypeArguments);
                    var actionType = typeof(Observer<>);
                    var actionTypeGeneric =
                        actionType.MakeGenericType(propertyInfo.PropertyType.GetTypeInfo().GenericTypeArguments);
                    var observer = Activator.CreateInstance(actionTypeGeneric, (Action<object>)(x => { tracker.TrackEvent(target.GetType(), propertyInfo, x); }));
                    subscribeGeneric.Invoke(null, new [] {observable, observer });
                }
            }
        }

        private static void Subscribe(object o)
        {
            
        }

        private class Observer<T> : IObserver<T>
        {
            private readonly Action<object> _action;

            public Observer(Action<object> action)
            {
                _action = action;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(T value)
            {
                _action(value);
            }
        }
    }

}
