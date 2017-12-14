using System;
using System.Collections.Generic;
using System.Linq;
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
            var invoker = new Add2Invoker(this);
            invoker.Value1 = value1;
            invoker.Value2 = value2;
            return invoker.Proceed();
        }

        private class Add2Invoker : IInvocation<int>
        {
            private readonly Class1 _class1;

            public int Value1;
            public int Value2;
            public Add2Invoker(Class1 class1)
            {
                _class1 = class1;
            }

            public int Proceed()
            {
                return _class1.Add2Inner(Value1, Value2);
            }

        }

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
