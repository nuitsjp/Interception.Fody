namespace Weaving
{
    public abstract class Invocation
    {
        public object[] Arguments { get; set; }

        public abstract object Proceed();

    }
}