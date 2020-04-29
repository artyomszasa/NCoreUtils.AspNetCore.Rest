using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.AspNetCore.Rest;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore
{
    public static class RestOperationAccessConfigurationBuilderExtensions
    {
        sealed class GenericAccessValidator : IAccessValidator
        {
            readonly Func<ClaimsPrincipal, CancellationToken, ValueTask<bool>> _callback;

            public GenericAccessValidator(Func<ClaimsPrincipal, CancellationToken, ValueTask<bool>> callback)
                => _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            public ValueTask<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
                => _callback(principal, cancellationToken);
        }

        sealed class GenericQueryAccessValidator : IQueryAccessValidator
        {
            readonly Func<IQueryable, ClaimsPrincipal, CancellationToken, ValueTask<IQueryable>> _callback;

            public GenericQueryAccessValidator(Func<IQueryable, ClaimsPrincipal, CancellationToken, ValueTask<IQueryable>> callback)
                => _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            public ValueTask<IQueryable> FilterQueryAsync(IQueryable source, ClaimsPrincipal principal, CancellationToken cancellationToken)
                => _callback(source, principal, cancellationToken);

            public ValueTask<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken) => new ValueTask<bool>(true);
        }

        public static IRestOperationAccessConfigurationBuilder Add(this IRestOperationAccessConfigurationBuilder builder, Func<IServiceProvider, IAccessValidator> factory)
        {
            return builder.Add(AccessValidatorDescriptor.Create(factory));
        }

        public static IRestOperationAccessConfigurationBuilder Add(this IRestOperationAccessConfigurationBuilder builder, Type type)
        {
            return builder.Add(AccessValidatorDescriptor.Create(type));
        }

        public static IRestOperationAccessConfigurationBuilder Add<TAccessValidator>(this IRestOperationAccessConfigurationBuilder builder)
            where TAccessValidator : IAccessValidator
            => builder.Add(AccessValidatorDescriptor.Create<TAccessValidator>());

        public static IRestOperationAccessConfigurationBuilder Add<TAccessValidator>(this IRestOperationAccessConfigurationBuilder builder, TAccessValidator validator)
            where TAccessValidator : IAccessValidator
            => builder.Add(AccessValidatorDescriptor.Create(validator));

        public static IRestOperationAccessConfigurationBuilder Use(this IRestOperationAccessConfigurationBuilder builder, Func<ClaimsPrincipal, CancellationToken, ValueTask<bool>> callback)
            => builder.Add(new GenericAccessValidator(callback));

        public static IRestOperationAccessConfigurationBuilder Use(this IRestOperationAccessConfigurationBuilder builder, Func<ClaimsPrincipal, bool> callback)
            => builder.Use((user, _) => new ValueTask<bool>(callback(user)));

        public static IRestOperationAccessConfigurationBuilder AllowAuthenticated(this IRestOperationAccessConfigurationBuilder builder)
            => builder.Use((user, _) => new ValueTask<bool>(user.Identity.IsAuthenticated));

        public static IRestOperationAccessConfigurationBuilder DenyAll(this IRestOperationAccessConfigurationBuilder builder)
            => builder.Use((_, __) => new ValueTask<bool>(false));

        public static IRestOperationAccessConfigurationBuilder<RestOperation.Query> UseFilter(
            this IRestOperationAccessConfigurationBuilder<RestOperation.Query> builder,
            Func<IQueryable, ClaimsPrincipal, CancellationToken, ValueTask<IQueryable>> callback)
        {
            builder.Add(new GenericQueryAccessValidator(callback));
            return builder;
        }

        public static IRestOperationAccessConfigurationBuilder<RestOperation.Query> UseFilter(
            this IRestOperationAccessConfigurationBuilder<RestOperation.Query> builder,
            Func<IQueryable, ClaimsPrincipal, IQueryable> callback)
            => builder.UseFilter((source, user, _) => new ValueTask<IQueryable>(callback(source, user)));
    }
}