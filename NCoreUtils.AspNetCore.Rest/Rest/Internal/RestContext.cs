using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public abstract class RestContext : IRestContext, ICloneable
{
    private readonly IRestContextMetadata metadata;

    protected RestContext(IRestContextMetadata metadata)
    {
        Preconditions.ThrowIfNull(metadata);
        this.metadata = metadata;
    }

    protected static T CloneOrPass<T>(T source)
        => source is ICloneable cloneable
            ? (T)cloneable.Clone()
            : source;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static RestCreateContext<TData, TId> Create<TData, TId>(TData data, IRestContextMetadata metadata)
        => new(data, metadata);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static RestDeleteContext<TData, TId> Delete<TData, TId>(TId id, bool force, IRestContextMetadata metadata)
        => new(id, force, metadata);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static RestItemContext<TData, TId> Item<TData, TId>(TId id, AsyncQueryFilter accessValidator, IRestContextMetadata metadata)
        => new(id, accessValidator, metadata);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static RestListCollectionContext<TData, TId> ListCollection<TData, TId>(RestQuery restQuery, AsyncQueryFilter accessValidator, IRestContextMetadata metadata)
        => new(restQuery, accessValidator, metadata);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static RestReductionContext<TData, TId> Reduction<TData, TId>(RestQuery restQuery, string reduction, AsyncQueryFilter accessValidator, IRestContextMetadata metadata)
        => new(restQuery, reduction, accessValidator, metadata);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static RestUpdateContext<TData, TId> Update<TData, TId>(TId id, TData data, IRestContextMetadata metadata)
        => new(id, data, metadata);

    public IRestContextMetadata Metadata => metadata;

    public abstract object Clone();
}

public class RestCreateContext<TData, TId> : RestContext
    , IRestCreateContext<TData, TId>
{
    public TData Data { get; }

    public RestCreateContext(TData data, IRestContextMetadata metadata) : base(metadata)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
    }

    public override object Clone()
        => Create<TData, TId>(CloneOrPass(Data), Metadata);
}

public class RestDeleteContext<TData, TId> : RestContext
    , IRestDeleteContext<TData, TId>
{
    public RestDeleteContext(TId id, bool force, IRestContextMetadata metadata) : base(metadata)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
        Force = force;
    }

    public TId Id { get; }

    public bool Force { get; }

    public override object Clone()
        => Delete<TData, TId>(CloneOrPass(Id), Force, Metadata);
}

public class RestItemContext<TData, TId> : RestContext
    , IRestItemContext<TData, TId>
{
    public TId Id { get; }

    public AsyncQueryFilter AccessValidator { get; }

    public RestItemContext(TId id, AsyncQueryFilter accessValidator, IRestContextMetadata metadata) : base(metadata)
    {
        ArgumentNullException.ThrowIfNull(id);
        Preconditions.ThrowIfNull(accessValidator);
        Id = id;
        AccessValidator = accessValidator;
    }

    public override object Clone()
        => Item<TData, TId>(CloneOrPass(Id), AccessValidator, Metadata);
}

public class RestListCollectionContext<TData, TId>(RestQuery restQuery, AsyncQueryFilter accessValidator, IRestContextMetadata metadata)
    : RestContext(metadata)
    , IRestListCollectionContext<TData, TId>
{
    public RestQuery RestQuery { get; } = restQuery ?? throw new ArgumentNullException(nameof(restQuery));

    public AsyncQueryFilter AccessValidator { get; } = accessValidator ?? throw new ArgumentNullException(nameof(accessValidator));

    public override object Clone() => this;
}

public class RestReductionContext<TData, TId>(RestQuery restQuery, string reduction, AsyncQueryFilter accessValidator, IRestContextMetadata metadata)
    : RestContext(metadata)
    , IRestReductionContext<TData, TId>
{
    public RestQuery RestQuery { get; } = restQuery ?? throw new ArgumentNullException(nameof(restQuery));

    public string Reduction { get; } = reduction ?? throw new ArgumentNullException(nameof(reduction));

    public AsyncQueryFilter AccessValidator { get; } = accessValidator ?? throw new ArgumentNullException(nameof(accessValidator));

    public override object Clone() => this;
}

public class RestUpdateContext<TData, TId> : RestContext
    , IRestUpdateContext<TData, TId>
{
    public TId Id { get; }

    public TData Data { get; }

    public RestUpdateContext(TId id, TData data, IRestContextMetadata metadata) : base(metadata)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(data);
        Id = id;
        Data = data;
    }

    public override object Clone()
        => Update(CloneOrPass(Id), CloneOrPass(Data), Metadata);
}