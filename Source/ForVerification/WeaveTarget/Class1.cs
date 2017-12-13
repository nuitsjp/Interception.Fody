using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeaveTarget
{
    public class Class1
    {
        public int Add(int left, int right)
        {
            int AddLocal(int arg1, int arg2)
            {
                return arg1 + arg2;
            }
            var invocation = new Invocation
            {
                Arguments = new object[]{left, right},
            };

            void InvokeAction()
            {
                invocation.ReturnValue = AddLocal(left, right);
            }

            invocation.InvokeAction = InvokeAction;
            return AddLocal(left, right);
        }
    }

    public interface IInvocation
    {
        object[] Arguments { get; set; }

        void Proceed();

        object ReturnValue { get; set; }
    }

    public class Invocation : IInvocation
    {
        public object[] Arguments { get; set; }

        public Action InvokeAction { get; set; }

        public void Proceed()
        {
            InvokeAction();
        }

        public object ReturnValue { get; set; }
    }

    public interface IInterceptor
    {
        void Intercept(IInvocation invocation);
    }

    public class ConcreteInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
        }
    }
}
