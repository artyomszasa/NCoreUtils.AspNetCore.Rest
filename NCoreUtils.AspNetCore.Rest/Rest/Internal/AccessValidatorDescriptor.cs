using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public readonly struct AccessValidatorDescriptor: IEquatable<AccessValidatorDescriptor>
    {
        public static bool operator==(AccessValidatorDescriptor a, AccessValidatorDescriptor b)
            => a.Equals(b);

        public static bool operator!=(AccessValidatorDescriptor a, AccessValidatorDescriptor b)
            => !a.Equals(b);

        public static AccessValidatorDescriptor CreateValidator([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            if (!typeof(IAccessStatusValidator).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"{typeof(IAccessStatusValidator)} is not assignable from {type}.");
            }
            return new AccessValidatorDescriptor(default, type, default, default, default, default);
        }

        [Obsolete("Use CreateValidator instead.")]
        public static AccessValidatorDescriptor Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            if (!typeof(IAccessValidator).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"{typeof(IAccessValidator)} is not assignable from {type}.");
            }
            return new AccessValidatorDescriptor(type, default, default);
        }

        public static AccessValidatorDescriptor CreateValidator<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
            where T : IAccessStatusValidator
            => CreateValidator(typeof(T));

        [Obsolete("Use CreateValidator instead.")]
        public static AccessValidatorDescriptor Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
            where T : IAccessValidator
            => Create(typeof(T));

        public static AccessValidatorDescriptor CreateValidator(IAccessStatusValidator instance)
            => new(default, default, default, instance, default, default);

        [Obsolete("Use CreateValidator instead.")]
        public static AccessValidatorDescriptor Create(IAccessValidator instance)
            => new(default, instance, default);

        public static AccessValidatorDescriptor CreateValidator(Func<IServiceProvider, IAccessStatusValidator> factory)
            => new(default, default, default, default, default, factory);

        [Obsolete("Use CreateValidator instead.")]
        public static AccessValidatorDescriptor Create(Func<IServiceProvider, IAccessValidator> factory)
            => new(default, default, factory);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        [Obsolete("Will be removed in next release.")]
        public Type? Type { get; }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type? ValidatorType { get; }

        [Obsolete("Will be removed in next release.")]
        public IAccessValidator? Instance { get; }

        public IAccessStatusValidator? ValidatorInstance { get; }

        [Obsolete("Will be removed in next release.")]
        public Func<IServiceProvider, IAccessValidator>? Factory { get; }

        public Func<IServiceProvider, IAccessStatusValidator>? ValidatorFactory { get; }

#pragma warning disable CS0618
        public bool IsEmpty => Type is null
            && ValidatorType is null
            && Instance is null
            && ValidatorInstance is null
            && Factory is null
            && ValidatorFactory is null;

        private AccessValidatorDescriptor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? validatorType,
            IAccessValidator? instance,
            IAccessStatusValidator? validatorInstance,
            Func<IServiceProvider, IAccessValidator>? factory,
            Func<IServiceProvider, IAccessStatusValidator>? validatorFactory)
        {
            Type = type;
            ValidatorType = validatorType;
            Instance = instance;
            ValidatorInstance = validatorInstance;
            Factory = factory;
            ValidatorFactory = validatorFactory;
        }
#pragma warning restore CS0618

        [Obsolete("Use factory methods instead.")]
        public AccessValidatorDescriptor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type,
            IAccessValidator? instance,
            Func<IServiceProvider, IAccessValidator>? factory)
        {
            Type = type;
            Instance = instance;
            Factory = factory;
        }

#pragma warning disable CS0618
        public bool Equals(AccessValidatorDescriptor other)
        {
            if (other.ValidatorType is null)
            {
                if (other.ValidatorInstance is null)
                {
                    if (other.ValidatorFactory is null)
                    {
                        if (other.Type is null)
                        {
                            if (other.Instance is null)
                            {
                                if (other.Factory is null)
                                {
                                    return IsEmpty;
                                }
                                return null != Factory && Factory.Equals(other.Factory);
                            }
                            return null != Instance && Instance.Equals(other.Instance);
                        }
                        return null != Type && Type.Equals(other.Type);
                    }
                    return ValidatorFactory is not null && ValidatorFactory.Equals(other.ValidatorFactory);
                }
                return ValidatorInstance is not null && ValidatorInstance.Equals(other.ValidatorInstance);
            }
            return ValidatorType is not null && ValidatorType.Equals(other.ValidatorType);
        }
#pragma warning restore CS0618

        public override bool Equals(object? obj)
            => obj is AccessValidatorDescriptor other && Equals(other);

#pragma warning disable CS0618
        public override int GetHashCode()
        {
            if (ValidatorType is not null)
            {
                return HashCode.Combine(-1, ValidatorType);
            }
            if (ValidatorInstance is not null)
            {
                return HashCode.Combine(-2, ValidatorInstance);
            }
            if (ValidatorFactory is not null)
            {
                return HashCode.Combine(-3, ValidatorFactory);
            }
            if (Type is null)
            {
                if (Instance is null)
                {
                    if (Factory is null)
                    {
                        return 0;
                    }
                    return HashCode.Combine(3, Factory);
                }
                return HashCode.Combine(2, Instance);
            }
            return HashCode.Combine(1, Type);
        }
#pragma warning restore CS0618

        [Obsolete("Use GetOrCreateValidator instead.")]
        public IAccessValidator CreateValidator(IServiceProvider serviceProvider, out bool mayRequireDisposal)
        {
            if (Type is null)
            {
                if (Instance is null)
                {
                    if (Factory is null)
                    {
                        mayRequireDisposal = false;
                        return NoAccessValidator.Instance;
                    }
                    mayRequireDisposal = true;
                    return Factory(serviceProvider);
                }
                mayRequireDisposal = false;
                return Instance;
            }
            mayRequireDisposal = true;
            return (IAccessValidator)ActivatorUtilities.CreateInstance(serviceProvider, Type);
        }

        public IAccessStatusValidator GetOrCreateValidator(IServiceProvider serviceProvider, out bool mayRequireDisposal)
        {
            if (ValidatorType is not null)
            {
                mayRequireDisposal = true;
                return (IAccessStatusValidator)ActivatorUtilities.CreateInstance(serviceProvider, ValidatorType);
            }
            if (ValidatorInstance is not null)
            {
                mayRequireDisposal = false;
                return ValidatorInstance;
            }
            if (ValidatorFactory is not null)
            {
                mayRequireDisposal = true;
                return ValidatorFactory(serviceProvider);
            }
#pragma warning disable CS0618
            return ValidatorAdapter.Adapt(CreateValidator(serviceProvider, out mayRequireDisposal));
#pragma warning restore CS0618
        }

        [Obsolete("Use TryGetOrCreateQueryValidator instead.")]
        public bool TryCreateQueryAccessValidator(
            IServiceProvider serviceProvider,
            out bool mayRequireDisposal,
            [NotNullWhen(true)] out IQueryAccessValidator? queryAccessValidator)
        {
            if (null != Type)
            {
                if (typeof(IQueryAccessValidator).IsAssignableFrom(Type))
                {
                    queryAccessValidator = (IQueryAccessValidator)ActivatorUtilities.CreateInstance(serviceProvider, Type);
                    mayRequireDisposal = true;
                    return true;
                }
                queryAccessValidator = default;
                mayRequireDisposal = default;
                return false;
            }
            if (null != Instance)
            {
                if (Instance is IQueryAccessValidator qav)
                {
                    queryAccessValidator = qav;
                    mayRequireDisposal = false;
                    return true;
                }
                queryAccessValidator = default;
                mayRequireDisposal = default;
                return false;
            }
            if (null != Factory)
            {
                var validator = Factory(serviceProvider);
                if (validator is IQueryAccessValidator qav)
                {
                    queryAccessValidator = qav;
                    mayRequireDisposal = true;
                    return true;
                }
                (validator as IDisposable)?.Dispose();
                queryAccessValidator = default;
                mayRequireDisposal = default;
                return false;
            }
            queryAccessValidator = default;
            mayRequireDisposal = default;
            return false;
        }

        public bool TryGetOrCreateQueryAccessValidator(
            IServiceProvider serviceProvider,
            out bool mayRequireDisposal,
            [NotNullWhen(true)] out IQueryAccessStatusValidator? queryAccessValidator)
        {
            if (ValidatorType is not null)
            {
                if (typeof(IQueryAccessStatusValidator).IsAssignableFrom(ValidatorType))
                {
                    queryAccessValidator = (IQueryAccessStatusValidator)ActivatorUtilities.CreateInstance(serviceProvider, ValidatorType);
                    mayRequireDisposal = true;
                    return true;
                }
                queryAccessValidator = default;
                mayRequireDisposal = default;
                return false;
            }
            if (ValidatorInstance is not null)
            {
                if (ValidatorInstance is IQueryAccessStatusValidator qav)
                {
                    queryAccessValidator = qav;
                    mayRequireDisposal = false;
                    return true;
                }
                queryAccessValidator = default;
                mayRequireDisposal = default;
                return false;
            }
            if (ValidatorFactory is not null)
            {
                var validator = ValidatorFactory(serviceProvider);
                if (validator is IQueryAccessStatusValidator qav)
                {
                    queryAccessValidator = qav;
                    mayRequireDisposal = true;
                    return true;
                }
                (validator as IDisposable)?.Dispose();
                queryAccessValidator = default;
                mayRequireDisposal = default;
                return false;
            }
#pragma warning disable CS0618
            if (TryCreateQueryAccessValidator(serviceProvider, out var deprecatedMayRequireDisposal, out var deprecatedValidator))
            {
                queryAccessValidator = QueryValidatorAdapter.Adapt(deprecatedValidator);
                mayRequireDisposal = deprecatedMayRequireDisposal;
                return true;
            }
#pragma warning restore CS0618
            queryAccessValidator = default;
            mayRequireDisposal = default;
            return false;
        }
    }
}