namespace Weaving
{
    public interface IInvocation
    {
        object[] Arguments { get; }
        object Invoke();
    }
}