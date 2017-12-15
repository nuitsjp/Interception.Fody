namespace Weaving
{
    public interface IInvocation
    {
        object[] Arguments { get; set; }
        object Invoke();
    }
}