namespace Weaving.Fody
{
    public enum PropertyType
    {
        // ReSharper disable once InconsistentNaming
        IReactiveProperty,
        // ReSharper disable once InconsistentNaming
        IReadOnlyReactiveProperty,
        ReactiveCommand,
        AsyncReactiveCommand,
        Other
    }
}