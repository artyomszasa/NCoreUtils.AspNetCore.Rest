using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NCoreUtils.AspNetCore.Rest;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore;

public static class RestEndpointOperationAccessConfigurationBuilderExtensions
{
    private sealed class GenericAccessValidator : IAccessStatusValidator
    {
        readonly Func<ClaimsPrincipal, CancellationToken, ValueTask<AccessStatusValidatorResult>> _callback;

        public GenericAccessValidator(Func<ClaimsPrincipal, CancellationToken, ValueTask<AccessStatusValidatorResult>> callback)
            => _callback = callback ?? throw new ArgumentNullException(nameof(callback));

        public ValueTask<AccessStatusValidatorResult> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
            => _callback(principal, cancellationToken);
    }

    sealed class GenericQueryAccessValidator : IQueryAccessStatusValidator
    {
        readonly Func<IQueryable, ClaimsPrincipal, CancellationToken, ValueTask<IQueryable>> _callback;

        public GenericQueryAccessValidator(Func<IQueryable, ClaimsPrincipal, CancellationToken, ValueTask<IQueryable>> callback)
            => _callback = callback ?? throw new ArgumentNullException(nameof(callback));

        public ValueTask<IQueryable> FilterQueryAsync(IQueryable source, ClaimsPrincipal principal, CancellationToken cancellationToken)
            => _callback(source, principal, cancellationToken);

        public ValueTask<AccessStatusValidatorResult> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
            => new(AccessStatusValidatorResult.Succeeded);
    }

    public static IRestEndpointOperationAccessConfigurationBuilder Add(this IRestEndpointOperationAccessConfigurationBuilder builder, Func<IServiceProvider, IAccessStatusValidator> factory)
    {
        return builder.Add(AccessValidatorDescriptor.CreateValidator(factory));
    }

    public static IRestEndpointOperationAccessConfigurationBuilder Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>(
        this IRestEndpointOperationAccessConfigurationBuilder builder)
        where TAccessValidator : IAccessStatusValidator
        => builder.Add(AccessValidatorDescriptor.CreateValidator<TAccessValidator>());

    public static IRestEndpointOperationAccessConfigurationBuilder Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>(
        this IRestEndpointOperationAccessConfigurationBuilder builder,
        TAccessValidator validator)
        where TAccessValidator : IAccessStatusValidator
        => builder.Add(AccessValidatorDescriptor.CreateValidator(validator));

    public static IRestEndpointOperationAccessConfigurationBuilder Use(
        this IRestEndpointOperationAccessConfigurationBuilder builder,
        Func<ClaimsPrincipal, CancellationToken, ValueTask<AccessStatusValidatorResult>> callback)
        => builder.Add(new GenericAccessValidator(callback));

    public static IRestEndpointOperationAccessConfigurationBuilder Use(
        this IRestEndpointOperationAccessConfigurationBuilder builder,
        Func<ClaimsPrincipal, AccessStatusValidatorResult> callback)
        => builder.Use((user, _) => new ValueTask<AccessStatusValidatorResult>(callback(user)));

    public static IRestEndpointOperationAccessConfigurationBuilder Use(
        this IRestEndpointOperationAccessConfigurationBuilder builder,
        Func<ClaimsPrincipal, bool> callback)
        => builder.Use((user, _) => new(callback(user)
            ? AccessStatusValidatorResult.Succeeded
            : AccessStatusValidatorResult.FallbackFailure
        ));

    public static IRestEndpointOperationAccessConfigurationBuilder AllowAuthenticated(this IRestEndpointOperationAccessConfigurationBuilder builder)
        => builder.Use((user, _) => new(user.Identity is not null && user.Identity.IsAuthenticated
            ? AccessStatusValidatorResult.Succeeded
            : AccessStatusValidatorResult.Failed(StatusCodes.Status401Unauthorized)
        ));

    public static IRestEndpointOperationAccessConfigurationBuilder DenyAll(this IRestEndpointOperationAccessConfigurationBuilder builder)
        => builder.Use((_, __) => new(AccessStatusValidatorResult.FallbackFailure));

    public static IRestEndpointOperationAccessConfigurationBuilder<RestOperation.Query> UseFilter(
        this IRestEndpointOperationAccessConfigurationBuilder<RestOperation.Query> builder,
        Func<IQueryable, ClaimsPrincipal, CancellationToken, ValueTask<IQueryable>> callback)
    {
        builder.Add(new GenericQueryAccessValidator(callback));
        return builder;
    }

    public static IRestEndpointOperationAccessConfigurationBuilder<RestOperation.Query> UseFilter(
        this IRestEndpointOperationAccessConfigurationBuilder<RestOperation.Query> builder,
        Func<IQueryable, ClaimsPrincipal, IQueryable> callback)
        => builder.UseFilter((source, user, _) => new ValueTask<IQueryable>(callback(source, user)));
}