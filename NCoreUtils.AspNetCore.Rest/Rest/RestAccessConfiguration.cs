using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest;

public class RestAccessConfiguration(
    AccessValidatorDescriptor create,
    AccessValidatorDescriptor update,
    AccessValidatorDescriptor delete,
    AccessValidatorDescriptor query)
{
    public static RestAccessConfiguration AllowAny { get; } = new RestAccessConfiguration(
        default,
        default,
        default,
        default
    );

    public AccessValidatorDescriptor Create { get; } = create;

    public AccessValidatorDescriptor Update { get; } = update;

    public AccessValidatorDescriptor Delete { get; } = delete;

    public AccessValidatorDescriptor Query { get; } = query;
}