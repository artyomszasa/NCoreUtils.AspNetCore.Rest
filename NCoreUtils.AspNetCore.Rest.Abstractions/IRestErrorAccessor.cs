using System.Runtime.ExceptionServices;

namespace NCoreUtils.AspNetCore.Rest;

public interface IRestErrorAccessor
{
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Intentional exact name match")]
    ExceptionDispatchInfo? Error { get; }
}