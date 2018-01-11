using System;
using System.Collections.Generic;
using System.Text;

namespace Weaving
{
    public sealed class TrackEventAttribute : Attribute
    {
        public Type EventTrackerType { get; }
        public TrackEventAttribute(Type eventTracker)
        {
            EventTrackerType = eventTracker;
        }
    }
}
