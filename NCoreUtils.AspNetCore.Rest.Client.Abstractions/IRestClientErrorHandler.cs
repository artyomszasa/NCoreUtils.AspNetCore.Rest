namespace NCoreUtils.Rest;

public interface IRestClientErrorHandler
{
    [SuppressMessage("Design", "CA1054:Uri parameters should not be strings",
        Justification = "Maintaining backward compatibility in public API.")]
    ValueTask ProcessAsync(
        HttpResponseMessage response,
        string requestUri,
        CancellationToken cancellationToken = default
    );
}