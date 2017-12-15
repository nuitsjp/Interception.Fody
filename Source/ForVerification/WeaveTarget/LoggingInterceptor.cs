using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Weaving;

namespace WeaveTarget
{
    public class LoggingInterceptor : IInterceptor
    {
        public object Intercept(IInvocation invocation)
        {
            Debug.WriteLine("Log");
            return invocation.Invoke();
        }
    }
}
