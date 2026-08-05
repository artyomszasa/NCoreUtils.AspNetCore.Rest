using System.Linq.Expressions;

namespace NCoreUtils.AspNetCore.Rest;

public interface IRestContextMetadata
{
    Expression<Func<TData, bool>> CreateIdEqualsPredicate<TData, TId>(TId id);
}

public interface IRestContext
{
    IRestContextMetadata Metadata { get; }
}

public interface IRestCreateContext<TData, TId> : IRestContext
{
    /// <summary>
    /// Object to insert into the dataset.
    /// </summary>
    TData Data { get; }

    Expression<Func<TData, bool>> CreateIdEqualsPredicate(TId id)
        => Metadata.CreateIdEqualsPredicate<TData, TId>(id);
}

public interface IRestDeleteContext<TData, TId> : IRestContext
{
    /// <summary>
    /// Id of the object to delete.
    /// </summary>
    TId Id { get; }

    /// <summary>
    /// Whether to perform forced removal, see
    /// <see cref="Data.IDataRepository{TData}.RemoveAsync(TData, bool, System.Threading.CancellationToken)" />.
    /// </summary>
    bool Force { get; }

    Expression<Func<TData, bool>> CreateIdEqualsPredicate(TId id)
        => Metadata.CreateIdEqualsPredicate<TData, TId>(id);
}

public interface IRestItemContext<TData, TId> : IRestContext
{
    /// <summary>
    /// Id of the object to return.
    /// </summary>
    TId Id { get; }

    /// <summary>
    /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
    /// </summary>
    AsyncQueryFilter AccessValidator { get; }

    Expression<Func<TData, bool>> CreateIdEqualsPredicate(TId id)
        => Metadata.CreateIdEqualsPredicate<TData, TId>(id);
}

public interface IRestListCollectionContext<TData> : IRestContext
{
    /// <summary>
    /// Query options specified in the request.
    /// </summary>
    RestQuery RestQuery { get; }

    /// <summary>
    /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
    /// </summary>
    AsyncQueryFilter AccessValidator { get; }
}

public interface IRestListCollectionContext<TData, TId> : IRestListCollectionContext<TData> { }

public interface IRestReductionContext<TData> : IRestContext
{
    /// <summary>
    /// Query options specified in the request.
    /// </summary>
    RestQuery RestQuery { get; }

    /// <summary>
    /// Reduction to perform.
    /// </summary>
    string Reduction { get; }

    /// <summary>
    /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
    /// </summary>
    AsyncQueryFilter AccessValidator { get; }
}

public interface IRestReductionContext<TData, TId> : IRestReductionContext<TData> { }

public interface IRestUpdateContext<TData, TId> : IRestContext
{
    /// <summary>
    /// Id of the object to update.
    /// </summary>
    TId Id { get; }

    /// <summary>
    /// Object to update in the dataset.
    /// </summary>
    TData Data { get; }

    Expression<Func<TData, bool>> CreateIdEqualsPredicate(TId id)
        => Metadata.CreateIdEqualsPredicate<TData, TId>(id);
}