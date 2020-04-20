using System.Linq;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality for filtering queryable of concrete type based on REST query.
    /// </summary>
    /// <typeparam name="T">Element type of the queryable.</typeparam>
    public interface IRestQueryFilter<T>
    {
        /// <summary>
        /// Filters queryable using provided REST query.
        /// </summary>
        /// <param name="source">Source queryable.</param>
        /// <param name="restQuery">REST query to apply.</param>
        /// <returns>Filtered queryable.</returns>
        IQueryable<T> ApplyFilters(IQueryable<T> source, RestQuery restQuery);
    }
}