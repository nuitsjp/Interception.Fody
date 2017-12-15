using System;
using System.Collections.Generic;
using System.Text;

namespace Weaving
{
    public class InterceptAttribute : Attribute
    {
        public Type[] InterceptorTypes { get; }

        public InterceptAttribute(params Type[] interceptorTypes)
        {
            InterceptorTypes = interceptorTypes;
        }
    }
}
