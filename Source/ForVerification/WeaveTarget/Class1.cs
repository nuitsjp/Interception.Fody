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
            //Func<int, int, int> func = (arg1, arg2) =>
            //{
            //    return arg1 + arg2;
            //};
            //return func(value1, value2);

            //int Func(int arg1, int arg2)
            //{
            //    return arg1 + arg2;
            //}

            //return Func(value1, value2);
            var type = typeof(Class1);
            var methodInfo = type.GetMethod("Add2Inner");
            var interceptorAttribute = methodInfo.GetCustomAttribute<InterceptAttribute>();
            var invocation = new Add2Invocation(interceptorAttribute.InterceptorTypes)
            {
                Class1 = this,
                Value1 = value1,
                Value2 = value2
            };
            return (int)invocation.Invoke();
        }

        private class Add2Invocation : Invocation
        {
            internal Class1 Class1;
            internal int Value1;
            internal int Value2;
            public override object[] Arguments => new object[]{Value1, Value2};

            internal Add2Invocation(Type[] interceptorTypes) : base(interceptorTypes)
            {
            }


            protected override object InvokeEndpoint()
            {
                return Class1.Add2Inner(Value1, Value2);
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
