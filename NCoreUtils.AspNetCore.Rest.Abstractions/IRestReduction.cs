namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Defines functionality to implement reduction extension for the concrete type.
/// </summary>
/// <typeparam name="T">Type of the target object.</typeparam>
public interface IRestReduction<T>
{
    /// <summary>
    /// Performes specified reduction for the predefined type with the specified parameters.
    /// </summary>
    /// <param name="context">Rest invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Reduction response that contains partial resultset defined by Offset and Count properties of the rest query
    /// parameter.
    /// </returns>
    ValueTask<object?> InvokeAsync(IRestReductionContext<T> context, CancellationToken cancellationToken);
}