using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public interface IEqualsPredicateFactory { }

public interface IEqualsPredicateFactory<TData, TId> : IEqualsPredicateFactory
{
    Expression<Func<TData, bool>> CreateIdEqualsPredicate(TId id);
}

public abstract class EndpointInvoker : IEqualsPredicateFactory
{
    public abstract Type DataType { get; }

    public abstract Type IdType { get; }

    public abstract IEqualsPredicateFactory IdEqualsPredicateFactory { get; }

    public abstract Task InvokeList(HttpContext httpContext, IRestContextMetadata metadata, RestAccessConfiguration accessConfiguration);

    public abstract Task InvokeItem(HttpContext httpContext, IRestContextMetadata metadata, object id, RestAccessConfiguration accessConfiguration);

    public abstract Task InvokeCreate(HttpContext httpContext, IRestContextMetadata metadata, RestAccessConfiguration accessConfiguration);

    public abstract Task InvokeUpdate(HttpContext httpContext, IRestContextMetadata metadata, object id, RestAccessConfiguration accessConfiguration);

    public abstract Task InvokeDelete(HttpContext httpContext, IRestContextMetadata metadata, object id, bool force, RestAccessConfiguration accessConfiguration);

    public abstract Task InvokeReduction(HttpContext httpContext, IRestContextMetadata metadata, string reduction, RestAccessConfiguration accessConfiguration);
}

public class EndpointInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>
    : EndpointInvoker
    , IEqualsPredicateFactory<TData, TId>
    where TData : class, IHasId<TId>
{
    public override Type DataType => typeof(TData);

    public override Type IdType => typeof(TId);

    public override IEqualsPredicateFactory IdEqualsPredicateFactory => this;

    [UnconditionalSuppressMessage("Trim", "IL2026", Justification = "IHasId<TId>.Id is preserved")]
    public virtual Expression<Func<TData, bool>> CreateIdEqualsPredicate(TId id)
    {
        var eArg = Expression.Parameter(typeof(TData));
        return Expression.Lambda<Func<TData, bool>>(
            Expression.Equal(
                Expression.Property(eArg, "Id"),
                QueryableExtensions.BoxConstant(id)
            ),
            eArg
        );
    }

    public override Task InvokeCreate(HttpContext httpContext, IRestContextMetadata metadata, RestAccessConfiguration accessConfiguration)
    {
        var serviceProvider = httpContext.RequestServices;
        var createInvoker = new CreateInvoker<TData, TId>(
            serviceProvider: serviceProvider,
            accessConfiguration: accessConfiguration,
            methodInvoker: serviceProvider.GetOptionalService<IRestMethodInvoker>(),
            implementation: serviceProvider.GetOptionalService<IRestCreate<TData, TId>>()
        );
        return createInvoker.Invoke(httpContext, metadata, httpContext.RequestAborted).AsTask();
    }

    public override Task InvokeDelete(HttpContext httpContext, IRestContextMetadata metadata, object id, bool force, RestAccessConfiguration accessConfiguration)
    {
        var serviceProvider = httpContext.RequestServices;
        var deleteInvoker = new DeleteInvoker<TData, TId>(
            serviceProvider: serviceProvider,
            accessConfiguration: accessConfiguration,
            methodInvoker: serviceProvider.GetOptionalService<IRestMethodInvoker>(),
            implementation: serviceProvider.GetOptionalService<IRestDelete<TData, TId>>()
        );
        return deleteInvoker.Invoke(httpContext, metadata, (TId)id, force, httpContext.RequestAborted).AsTask();
    }

    public override Task InvokeItem(HttpContext httpContext, IRestContextMetadata metadata, object id, RestAccessConfiguration accessConfiguration)
    {
        var serviceProvider = httpContext.RequestServices;
        var itemInvoker = new ItemInvoker<TData, TId>(
            serviceProvider: serviceProvider,
            accessConfiguration: accessConfiguration,
            methodInvoker: serviceProvider.GetOptionalService<IRestMethodInvoker>(),
            implementation: serviceProvider.GetOptionalService<IRestItem<TData, TId>>(),
            serializerFactory: serviceProvider.GetOptionalService<ISerializerFactory>()
        );
        return itemInvoker.Invoke(httpContext, metadata, (TId)id, httpContext.RequestAborted).AsTask();
    }

    public override Task InvokeList(HttpContext httpContext, IRestContextMetadata metadata, RestAccessConfiguration accessConfiguration)
    {
        var serviceProvider = httpContext.RequestServices;
        var listInvoker = new ListInvoker<TData, TId>(
            serviceProvider: serviceProvider,
            accessConfiguration: accessConfiguration,
            logger: serviceProvider.GetRequiredService<ILogger<ListInvoker>>(),
            queryParser: serviceProvider.GetOptionalService<IRestQueryParser>(),
            methodInvoker: serviceProvider.GetOptionalService<IRestMethodInvoker>(),
            implementation: serviceProvider.GetOptionalService<IRestListCollection<TData>>(),
            serializerFactory: serviceProvider.GetOptionalService<ISerializerFactory>()
        );
        return listInvoker.Invoke(httpContext, metadata, httpContext.RequestAborted).AsTask();
    }

    public override Task InvokeReduction(HttpContext httpContext, IRestContextMetadata metadata, string reduction, RestAccessConfiguration accessConfiguration)
    {
        var serviceProvider = httpContext.RequestServices;
        var reductionInvoker = new ReductionInvoker<TData, TId>(
            serviceProvider: serviceProvider,
            accessConfiguration: accessConfiguration,
            queryParser: serviceProvider.GetOptionalService<IRestQueryParser>(),
            methodInvoker: serviceProvider.GetOptionalService<IRestMethodInvoker>(),
            implementation: serviceProvider.GetOptionalService<IRestReduction<TData>>(),
            serializerFactory: serviceProvider.GetOptionalService<ISerializerFactory>()
        );
        return reductionInvoker.Invoke(httpContext, metadata, reduction, httpContext.RequestAborted).AsTask();
    }

    public override Task InvokeUpdate(HttpContext httpContext, IRestContextMetadata metadata, object id, RestAccessConfiguration accessConfiguration)
    {
        var serviceProvider = httpContext.RequestServices;
        var updateInvoker = new UpdateInvoker<TData, TId>(
            serviceProvider: serviceProvider,
            accessConfiguration: accessConfiguration,
            methodInvoker: serviceProvider.GetOptionalService<IRestMethodInvoker>(),
            implementation: serviceProvider.GetOptionalService<IRestUpdate<TData, TId>>(),
            deserializer: serviceProvider.GetOptionalService<IDeserializer<TData>>()
        );
        return updateInvoker.Invoke(httpContext, metadata, (TId)id, httpContext.RequestAborted).AsTask();
    }
}

public class EndpointInvokerInt32<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData>
    : EndpointInvoker<TData, int>
    where TData : class, IHasId<int>
{
    public override Expression<Func<TData, bool>> CreateIdEqualsPredicate(int id)
        => e => e.Id == id;
}

public class EndpointInvokerString<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData>
    : EndpointInvoker<TData, string>
    where TData : class, IHasId<string>
{
    public override Expression<Func<TData, bool>> CreateIdEqualsPredicate(string id)
        => e => e.Id == id;
}

public class EndpointInvokerGuid<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData>
    : EndpointInvoker<TData, Guid>
    where TData : class, IHasId<Guid>
{
    public override Expression<Func<TData, bool>> CreateIdEqualsPredicate(Guid id)
        => e => e.Id == id;
}