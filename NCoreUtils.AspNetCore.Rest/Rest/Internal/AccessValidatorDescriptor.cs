using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.AspNetCore.Rest.Internal;

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
        return new AccessValidatorDescriptor(type, default, default);
    }

    public static AccessValidatorDescriptor CreateValidator<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : IAccessStatusValidator
        => CreateValidator(typeof(T));

    public static AccessValidatorDescriptor CreateValidator(IAccessStatusValidator instance)
        => new(default, instance, default);

    public static AccessValidatorDescriptor CreateValidator(Func<IServiceProvider, IAccessStatusValidator> factory)
        => new(default, default, factory);

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type? ValidatorType { get; }

    public IAccessStatusValidator? ValidatorInstance { get; }

    public Func<IServiceProvider, IAccessStatusValidator>? ValidatorFactory { get; }

    public bool IsEmpty => ValidatorType is null
        && ValidatorInstance is null
        && ValidatorFactory is null;

    private AccessValidatorDescriptor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? validatorType,
        IAccessStatusValidator? validatorInstance,
        Func<IServiceProvider, IAccessStatusValidator>? validatorFactory)
    {
        ValidatorType = validatorType;
        ValidatorInstance = validatorInstance;
        ValidatorFactory = validatorFactory;
    }

    public bool Equals(AccessValidatorDescriptor other)
    {
        if (other.ValidatorType is null)
        {
            if (other.ValidatorInstance is null)
            {
                if (other.ValidatorFactory is null)
                {
                    return IsEmpty == other.IsEmpty;
                }
                return ValidatorFactory is not null && ValidatorFactory.Equals(other.ValidatorFactory);
            }
            return ValidatorInstance is not null && ValidatorInstance.Equals(other.ValidatorInstance);
        }
        return ValidatorType is not null && ValidatorType.Equals(other.ValidatorType);
    }

    public override bool Equals(object? obj)
        => obj is AccessValidatorDescriptor other && Equals(other);

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
        return 0;
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
        mayRequireDisposal = false;
        return NoAccessValidator.Singleton;
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
        queryAccessValidator = default;
        mayRequireDisposal = default;
        return false;
    }
}