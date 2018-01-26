using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Weaving
{
    public abstract class EventTrackerManagerBase
    {
        public IEventTracker GlobalEventTracker { get; set; }

        protected EventTrackerManagerBase()
        {
        }
    }
}
