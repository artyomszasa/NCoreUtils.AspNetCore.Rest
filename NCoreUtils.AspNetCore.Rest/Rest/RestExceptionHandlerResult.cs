using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.AspNetCore.Rest;

[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not required as the struct is never compared directly.")]
public readonly struct RestExceptionHandlerResult
{
    internal enum States
    {
        Unhandled = 0,
        Handled = 1,
        Pass = 2
    }

    public static RestExceptionHandlerResult Unhandled { get; } = new(States.Unhandled);

    public static RestExceptionHandlerResult Handled { get; } = new(States.Handled);

    public static RestExceptionHandlerResult Pass { get; } = new(States.Pass);

    internal States State { get; }

    private RestExceptionHandlerResult(States state)
        => State = state;
}