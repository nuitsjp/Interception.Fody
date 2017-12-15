namespace Weaving
{
    public interface IInterceptor
    {
        object Intercept(IInvocation invocation);
    }
}