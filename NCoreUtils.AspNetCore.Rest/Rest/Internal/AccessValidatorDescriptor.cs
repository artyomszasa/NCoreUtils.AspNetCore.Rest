using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public struct AccessValidatorDescriptor: IEquatable<AccessValidatorDescriptor>
    {
        public static bool operator==(AccessValidatorDescriptor a, AccessValidatorDescriptor b)
            => a.Equals(b);

        public static bool operator!=(AccessValidatorDescriptor a, AccessValidatorDescriptor b)
            => !a.Equals(b);

        public static AccessValidatorDescriptor Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            if (!typeof(IAccessValidator).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"{typeof(IAccessValidator)} is not assignable from {type}.");
            }
            return new AccessValidatorDescriptor(type, default, default);
        }

        public static AccessValidatorDescriptor Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
            where T : IAccessValidator
            => Create(typeof(T));

        public static AccessValidatorDescriptor Create(IAccessValidator instance)
            => new AccessValidatorDescriptor(default, instance, default);

        public static AccessValidatorDescriptor Create(Func<IServiceProvider, IAccessValidator> factory)
            => new AccessValidatorDescriptor(default, default, factory);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type? Type { get; }

        public IAccessValidator? Instance { get; }

        public Func<IServiceProvider, IAccessValidator>? Factory { get; }

        public bool IsEmpty => Type is null && Instance is null && Factory is null;

        public AccessValidatorDescriptor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type,
            IAccessValidator? instance,
            Func<IServiceProvider, IAccessValidator>? factory)
        {
            Type = type;
            Instance = instance;
            Factory = factory;
        }

        public bool Equals(AccessValidatorDescriptor other)
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

        public override bool Equals(object? obj)
            => obj is AccessValidatorDescriptor other && Equals(other);

        public override int GetHashCode()
        {
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
    }
}