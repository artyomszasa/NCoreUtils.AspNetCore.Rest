using System.Linq;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality for ordering queryable of concrete type based on REST query.
    /// </summary>
    /// <typeparam name="T">Element type of the queryable.</typeparam>
    public interface IRestQueryOrderer<T>
    {
        /// <summary>
        /// Orders queryable using provided REST query.
        /// </summary>
        /// <param name="source">Source queryable.</param>
        /// <param name="restQuery">REST query to apply.</param>
        /// <returns>Ordered queryable.</returns>
        IOrderedQueryable<T> ApplyOrder(IQueryable<T> source, RestQuery restQuery);
    }
}