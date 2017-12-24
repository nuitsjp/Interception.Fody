namespace Interception
{
    public interface IInvocation
    {
        object[] Arguments { get; }
        object Invoke();
    }
}