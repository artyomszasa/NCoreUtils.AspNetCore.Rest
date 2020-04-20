namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality for retrieving default ordering property from concrete type.
    /// </summary>
    /// <typeparam name="T">Type of the underlying output target.</typeparam>
    public interface IDefaultOrderProperty<T>
    {
        /// <summary>
        /// Retrieves default ordering property for the predefined type.
        /// </summary>
        /// <returns>Order by property descriptor.</returns>
        OrderByProperty Select();
    }
}