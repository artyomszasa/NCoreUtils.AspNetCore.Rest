using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Defines functionality for retrieving default ordering property from concrete type.
/// </summary>
/// <typeparam name="T">Type of the underlying output target.</typeparam>
public interface IDefaultOrderProperty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>
{
    /// <summary>
    /// Retrieves default ordering property for the predefined type.
    /// </summary>
    /// <returns>Order by property descriptor.</returns>
#pragma warning disable CA1716 // Identifiers should not match keywords
    OrderByProperty Select();
#pragma warning restore CA1716 // Identifiers should not match keywords
}