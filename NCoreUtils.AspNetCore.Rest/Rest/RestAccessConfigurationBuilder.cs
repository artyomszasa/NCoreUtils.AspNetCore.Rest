using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest
{
    public class RestAccessConfigurationBuilder
    {
        sealed class GlobalAccessConfigurationBuilder : IRestOperationAccessConfigurationBuilder
        {
            readonly RestAccessConfigurationBuilder _target;

            public GlobalAccessConfigurationBuilder(RestAccessConfigurationBuilder target)
                => _target = target;

            public IRestOperationAccessConfigurationBuilder Add(AccessValidatorDescriptor factory)
            {
                _target.Create.Add(factory);
                _target.Update.Add(factory);
                _target.Delete.Add(factory);
                _target.Query.Add(factory);
                return this;
            }
        }

        sealed class OperationAccessConfigurationBuilder<TOperation> : IRestOperationAccessConfigurationBuilder<TOperation>
            where TOperation : RestOperation
        {
            readonly List<AccessValidatorDescriptor> _factories;

            public OperationAccessConfigurationBuilder(List<AccessValidatorDescriptor> factories)
                => _factories = factories;

            public IRestOperationAccessConfigurationBuilder Add(AccessValidatorDescriptor factory)
            {
                _factories.Add(factory);
                return this;
            }
        }

        sealed class CompositeAccessValidator : IQueryAccessValidator
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
                    if (descriptor.TryCreateQueryAccessValidator(_serviceProvider, out var mayRequireDisposal, out var queryAccessValidator))
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

            public async ValueTask<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
            {
                foreach (var descriptor in _descriptors)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var validator = descriptor.CreateValidator(_serviceProvider, out var mayRequireDisposal);
                    try
                    {
                        var pass = await validator.ValidateAsync(principal, cancellationToken);
                        if (!pass)
                        {
                            return false;
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
                return true;
            }
        }

        static AccessValidatorDescriptor BuildFromList(IReadOnlyList<AccessValidatorDescriptor> source)
        {
            switch (source.Count)
            {
                case 0:
                    return default;
                case 1:
                    return source[0];
                default:
                    var descriptors = source.ToImmutableArray();
                    return AccessValidatorDescriptor.Create(serviceProvider => new CompositeAccessValidator(serviceProvider, descriptors));
            }
        }

        public List<AccessValidatorDescriptor> Create { get; } = new List<AccessValidatorDescriptor>();

        public List<AccessValidatorDescriptor> Update { get; } = new List<AccessValidatorDescriptor>();

        public List<AccessValidatorDescriptor> Delete { get; } = new List<AccessValidatorDescriptor>();

        public List<AccessValidatorDescriptor> Query { get; } = new List<AccessValidatorDescriptor>();

        public RestAccessConfigurationBuilder ConfigureGlobal(Action<IRestOperationAccessConfigurationBuilder> configure)
        {
            configure(new GlobalAccessConfigurationBuilder(this));
            return this;
        }

        public RestAccessConfigurationBuilder ConfigureCreate(Action<IRestOperationAccessConfigurationBuilder<RestOperation.Create>> configure)
        {
            configure(new OperationAccessConfigurationBuilder<RestOperation.Create>(Create));
            return this;
        }

        public RestAccessConfigurationBuilder ConfigureUpdate(Action<IRestOperationAccessConfigurationBuilder<RestOperation.Update>> configure)
        {
            configure(new OperationAccessConfigurationBuilder<RestOperation.Update>(Update));
            return this;
        }

        public RestAccessConfigurationBuilder ConfigureDelete(Action<IRestOperationAccessConfigurationBuilder<RestOperation.Delete>> configure)
        {
            configure(new OperationAccessConfigurationBuilder<RestOperation.Delete>(Delete));
            return this;
        }

        public RestAccessConfigurationBuilder ConfigureQuery(Action<IRestOperationAccessConfigurationBuilder<RestOperation.Query>> configure)
        {
            configure(new OperationAccessConfigurationBuilder<RestOperation.Query>(Query));
            return this;
        }

        public RestAccessConfiguration Build()
            => new RestAccessConfiguration(
                create: BuildFromList(Create),
                update: BuildFromList(Update),
                delete: BuildFromList(Delete),
                query: BuildFromList(Query)
            );

        private sealed class AccessValidationAdder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>
            where TAccessValidator : IAccessValidator
        {
            public static void Add(IRestOperationAccessConfigurationBuilder b) => b.Add<TAccessValidator>();

            public static void Add<TOperation>(IRestOperationAccessConfigurationBuilder<TOperation> b)
                where TOperation : RestOperation
                => b.Add<TAccessValidator>();
        }

        public RestAccessConfigurationBuilder RestrictAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>()
            where TAccessValidator : IAccessValidator
            => this.ConfigureGlobal(AccessValidationAdder<TAccessValidator>.Add);

        public RestAccessConfigurationBuilder RestrictAll(Func<ClaimsPrincipal, CancellationToken, ValueTask<bool>> callback)
            => this.ConfigureGlobal(b => b.Use(callback));

        public RestAccessConfigurationBuilder RestrictAll(Func<ClaimsPrincipal, bool> callback)
            => this.ConfigureGlobal(b => b.Use(callback));

        public RestAccessConfigurationBuilder RestrictCreate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>()
            where TAccessValidator : IAccessValidator
            => this.ConfigureCreate(AccessValidationAdder<TAccessValidator>.Add);

        public RestAccessConfigurationBuilder RestrictCreate(Func<ClaimsPrincipal, CancellationToken, ValueTask<bool>> callback)
            => this.ConfigureCreate(b => b.Use(callback));

        public RestAccessConfigurationBuilder RestrictCreate(Func<ClaimsPrincipal, bool> callback)
            => this.ConfigureCreate(b => b.Use(callback));

        public RestAccessConfigurationBuilder RestrictUpdate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>()
            where TAccessValidator : IAccessValidator
            => this.ConfigureUpdate(AccessValidationAdder<TAccessValidator>.Add);

        public RestAccessConfigurationBuilder RestrictUpdate(Func<ClaimsPrincipal, CancellationToken, ValueTask<bool>> callback)
            => this.ConfigureUpdate(b => b.Use(callback));

        public RestAccessConfigurationBuilder RestrictUpdate(Func<ClaimsPrincipal, bool> callback)
            => this.ConfigureUpdate(b => b.Use(callback));

        public RestAccessConfigurationBuilder RestrictDelete<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccessValidator>()
            where TAccessValidator : IAccessValidator
            => this.ConfigureDelete(AccessValidationAdder<TAccessValidator>.Add);

        public RestAccessConfigurationBuilder RestrictDelete(Func<ClaimsPrincipal, CancellationToken, ValueTask<bool>> callback)
            => this.ConfigureDelete(b => b.Use(callback));

        public RestAccessConfigurationBuilder RestrictDelete(Func<ClaimsPrincipal, bool> callback)
            => this.ConfigureDelete(b => b.Use(callback));
    }
}