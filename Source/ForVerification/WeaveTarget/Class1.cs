using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Weaving;

namespace WeaveTarget
{
    public class Class1
    {
        public int Add(int left, int right)
        {
            return left + right;
        }

        //public int AddInner(int left, int right)
        //{
        //    return left + right;
        //}

        public int Add2(int value1, int value2)
        {
            var type = typeof(Class1);
            var methodInfo = type.GetMethod("Add2Inner");
            var interceptorAttribute = methodInfo.GetCustomAttribute<InterceptAttribute>();
            var invocation = new Add2Invocation(interceptorAttribute.InterceptorTypes, this)
            {
                Value1 = value1,
                Value2 = value2
            };
            return (int)invocation.Invoke();
        }

        private class Add2Invocation : Invocation
        {
            private readonly Class1 _class1;

            public int Value1;
            public int Value2;

            public Add2Invocation(Type[] interceptorTypes, Class1 class1) : base(interceptorTypes)
            {
                _class1 = class1;
            }

            public override object InvokeEndpoint()
            {
                return _class1.Add2Inner(Value1, Value2);
            }
        }

        [Intercept(typeof(LoggingInterceptor))]
        public int Add2Inner(int value1, int value2)
        {
            return value1 + value2;
        }

        //public int Add(int left, int right)
        //{
        //    return AddInner(left, right);
        //}

        //public int AddInner(int left, int right)
        //{
        //    return left + right;
        //}
    }

}
