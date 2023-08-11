using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest;

    public interface IRestEndpointOperationAccessConfigurationBuilder
    {
        IRestEndpointOperationAccessConfigurationBuilder Add(AccessValidatorDescriptor descriptor);
    }

    public interface IRestEndpointOperationAccessConfigurationBuilder<TOperation> : IRestEndpointOperationAccessConfigurationBuilder
        where TOperation : RestOperation
    {
        new IRestEndpointOperationAccessConfigurationBuilder<TOperation> Add(AccessValidatorDescriptor descriptor);

        IRestEndpointOperationAccessConfigurationBuilder IRestEndpointOperationAccessConfigurationBuilder.Add(AccessValidatorDescriptor descriptor)
            => Add(descriptor);
    }

public class RestEndpointsAccessConfigurationBuilder
{
    private sealed class GlobalAccessConfigurationBuilder : IRestEndpointOperationAccessConfigurationBuilder
    {
        readonly RestEndpointsAccessConfigurationBuilder _target;

        public GlobalAccessConfigurationBuilder(RestEndpointsAccessConfigurationBuilder target)
            => _target = target;

        public IRestEndpointOperationAccessConfigurationBuilder Add(AccessValidatorDescriptor descriptor)
        {
            _target.Create.Add(descriptor);
            _target.Update.Add(descriptor);
            _target.Delete.Add(descriptor);
            _target.Query.Add(descriptor);
            return this;
        }
    }

    private sealed class OperationAccessConfigurationBuilder<TOperation> : IRestEndpointOperationAccessConfigurationBuilder<TOperation>
        where TOperation : RestOperation
    {
        readonly List<AccessValidatorDescriptor> _factories;

        public OperationAccessConfigurationBuilder(List<AccessValidatorDescriptor> factories)
            => _factories = factories;

        public IRestEndpointOperationAccessConfigurationBuilder<TOperation> Add(AccessValidatorDescriptor descriptor)
        {
            _factories.Add(descriptor);
            return this;
        }
    }

    private sealed class CompositeAccessValidator : IQueryAccessStatusValidator
    {
        readonly IServiceProvider _serviceProvider;

        readonly ImmutableArray<AccessValidatorDescriptor> _descriptors;

        public CompositeAccessValidator(IServiceProvider serviceProvider, ImmutableArray<AccessValidatorDescriptor> descriptors)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _descriptors = descriptors;
        }

        public async ValueTask<IQueryable> FilterQueryAsync(IQueryable source, ClaimsPrincipal principal, CancellationToken cancellationToken)
        {
            var result = source;
            foreach (var descriptor in _descriptors)
            {
                if (descriptor.TryGetOrCreateQueryAccessValidator(_serviceProvider, out var mayRequireDisposal, out var queryAccessValidator))
                {
                    result = await queryAccessValidator.FilterQueryAsync(result, principal, cancellationToken);
                    if (mayRequireDisposal)
                    {
                        (queryAccessValidator as IDisposable)?.Dispose();
                    }
                }
            }
            return result;
        }

        public async ValueTask<AccessStatusValidatorResult> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
        {
            foreach (var descriptor in _descriptors)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var validator = descriptor.GetOrCreateValidator(_serviceProvider, out var mayRequireDisposal);
                try
                {
                    var pass = await validator.ValidateAsync(principal, cancellationToken);
                    if (!pass.Success)
                    {
                        return pass;
                    }
                }
                finally
                {
                    if (mayRequireDisposal)
                    {
                        (validator as IDisposable)?.Dispose();
                    }
                }
            }
            return AccessStatusValidatorResult.Succeeded;
        }
    }

    private static AccessValidatorDescriptor BuildFromList(IReadOnlyList<AccessValidatorDescriptor> source)
    {
        switch (source.Count)
        {
            case 0:
                return default;
            case 1:
                return source[0];
            default:
                var descriptors = source.ToImmutableArray();
                return AccessValidatorDescriptor.CreateValidator(serviceProvider => new CompositeAccessValidator(serviceProvider, descriptors));
        }
    }

    public List<AccessValidatorDescriptor> Create { get; } = new List<AccessValidatorDescriptor>();

    public List<AccessValidatorDescriptor> Update { get; } = new List<AccessValidatorDescriptor>();

    public List<AccessValidatorDescriptor> Delete { get; } = new List<AccessValidatorDescriptor>();

    public List<AccessValidatorDescriptor> Query { get; } = new List<AccessValidatorDescriptor>();

    public RestEndpointsAccessConfigurationBuilder ConfigureGlobal(Action<IRestEndpointOperationAccessConfigurationBuilder> configure)
    {
        configure(new GlobalAccessConfigurationBuilder(this));
        return this;
    }

    public RestEndpointsAccessConfigurationBuilder ConfigureCreate(Action<IRestEndpointOperationAccessConfigurationBuilder<RestOperation.Create>> configure)
    {
        configure(new OperationAccessConfigurationBuilder<RestOperation.Create>(Create));
        return this;
    }

    public RestEndpointsAccessConfigurationBuilder ConfigureUpdate(Action<IRestEndpointOperationAccessConfigurationBuilder<RestOperation.Update>> configure)
    {
        configure(new OperationAccessConfigurationBuilder<RestOperation.Update>(Update));
        return this;
    }

    public RestEndpointsAccessConfigurationBuilder ConfigureDelete(Action<IRestEndpointOperationAccessConfigurationBuilder<RestOperation.Delete>> configure)
    {
        configure(new OperationAccessConfigurationBuilder<RestOperation.Delete>(Delete));
        return this;
    }

    public RestEndpointsAccessConfigurationBuilder ConfigureQuery(Action<IRestEndpointOperationAccessConfigurationBuilder<RestOperation.Query>> configure)
    {
        configure(new OperationAccessConfigurationBuilder<RestOperation.Query>(Query));
        return this;
    }

    public RestAccessConfiguration Build() => new(
        create: BuildFromList(Create),
        update: BuildFromList(Update),
        delete: BuildFromList(Delete),
        query: BuildFromList(Query)
    );

    private sealed class AccessValidationAdder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>
        where TAccessValidator : IAccessStatusValidator
    {
        public static void Add(IRestEndpointOperationAccessConfigurationBuilder b) => b.Add<TAccessValidator>();

        public static void Add<TOperation>(IRestEndpointOperationAccessConfigurationBuilder<TOperation> b)
            where TOperation : RestOperation
            => b.Add<TAccessValidator>();
    }

    public RestEndpointsAccessConfigurationBuilder RestrictAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>()
        where TAccessValidator : IAccessStatusValidator
        => ConfigureGlobal(AccessValidationAdder<TAccessValidator>.Add);

    public RestEndpointsAccessConfigurationBuilder RestrictAll(Func<ClaimsPrincipal, CancellationToken, ValueTask<AccessStatusValidatorResult>> callback)
        => ConfigureGlobal(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictAll(Func<ClaimsPrincipal, AccessStatusValidatorResult> callback)
        => ConfigureGlobal(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictAll(Func<ClaimsPrincipal, bool> callback)
        => ConfigureGlobal(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictCreate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>()
        where TAccessValidator : IAccessStatusValidator
        => ConfigureCreate(AccessValidationAdder<TAccessValidator>.Add);

    public RestEndpointsAccessConfigurationBuilder RestrictCreate(Func<ClaimsPrincipal, CancellationToken, ValueTask<AccessStatusValidatorResult>> callback)
        => ConfigureCreate(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictCreate(Func<ClaimsPrincipal, AccessStatusValidatorResult> callback)
        => ConfigureCreate(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictCreate(Func<ClaimsPrincipal, bool> callback)
        => ConfigureCreate(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictUpdate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>()
        where TAccessValidator : IAccessStatusValidator
        => ConfigureUpdate(AccessValidationAdder<TAccessValidator>.Add);

    public RestEndpointsAccessConfigurationBuilder RestrictUpdate(Func<ClaimsPrincipal, CancellationToken, ValueTask<AccessStatusValidatorResult>> callback)
        => ConfigureUpdate(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictUpdate(Func<ClaimsPrincipal, AccessStatusValidatorResult> callback)
        => ConfigureUpdate(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictUpdate(Func<ClaimsPrincipal, bool> callback)
        => ConfigureUpdate(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictDelete<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>()
        where TAccessValidator : IAccessStatusValidator
        => ConfigureDelete(AccessValidationAdder<TAccessValidator>.Add);

    public RestEndpointsAccessConfigurationBuilder RestrictDelete(Func<ClaimsPrincipal, CancellationToken, ValueTask<AccessStatusValidatorResult>> callback)
        => ConfigureDelete(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictDelete(Func<ClaimsPrincipal, AccessStatusValidatorResult> callback)
        => ConfigureDelete(b => b.Use(callback));

    public RestEndpointsAccessConfigurationBuilder RestrictDelete(Func<ClaimsPrincipal, bool> callback)
        => ConfigureDelete(b => b.Use(callback));
}