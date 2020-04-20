using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest
{
    public class RestAccessConfiguration
    {
        public static RestAccessConfiguration AllowAny { get; } = new RestAccessConfiguration(
            default,
            default,
            default,
            default
        );

        public AccessValidatorDescriptor Create { get; }

        public AccessValidatorDescriptor Update { get; }

        public AccessValidatorDescriptor Delete { get; }

        public AccessValidatorDescriptor Query { get; }

        public RestAccessConfiguration(
            AccessValidatorDescriptor create,
            AccessValidatorDescriptor update,
            AccessValidatorDescriptor delete,
            AccessValidatorDescriptor query)
        {
            Create = create;
            Update = update;
            Delete = delete;
            Query = query;
        }
    }
}