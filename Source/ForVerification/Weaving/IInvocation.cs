namespace Weaving
{
    public interface IInvocation<out T>
    {
        T Proceed();
    }

}