using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest;

public sealed partial class RestEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
{
    private static class Segments
    {
        public static RoutePattern Combine(RoutePatternPathSegment? parentSegment, params string[] args)
        {
            // var segment = RoutePatternFactory.Segment(RoutePatternFactory.LiteralPart(rawSegment));
            // return parentSegment is null
            //     ? RoutePatternFactory.Pattern(segment)
            //     : RoutePatternFactory.Pattern(parentSegment, segment);
            if (parentSegment is null)
            {
                return RoutePatternFactory.Pattern(args.Select(arg => RoutePatternFactory.Segment(RoutePatternFactory.ParameterPart(arg))));
            }
            return RoutePatternFactory.Pattern(args.Select(arg => RoutePatternFactory.Segment(RoutePatternFactory.ParameterPart(arg))).Prepend(parentSegment));
        }
    }

    private static class Operations
    {
        public const string Collection = "COLLECTION";

        public const string Item = "ITEM";

        public const string Reduction = "REDUCTION";

        public const string Create = "CREATE";

        public const string Update = "UPDATE";

        public const string Delete = "DELETE";
    }

    private sealed class RestContextMetadata(IReadOnlyDictionary<Type, EndpointInvoker> invokers) : IRestContextMetadata
    {
        private IReadOnlyDictionary<Type, EndpointInvoker> Invokers { get; } = invokers;

        public Expression<Func<TData, bool>> CreateIdEqualsPredicate<TData, TId>(TId id)
        {
            if (Invokers.TryGetValue(typeof(TData), out var invoker))
            {
                return ((IEqualsPredicateFactory<TData, TId>)invoker.IdEqualsPredicateFactory)
                    .CreateIdEqualsPredicate(id);
            }
            throw new InvalidOperationException($"No invoker for {typeof(TData).Name}.");
        }
    }

    private const string TagOperation = "operation";

    private static bool IsTruthy(string? value)
        => value switch
        {
            null => false,
            "true" => true,
            "t" => true,
            "1" => true,
            "on" => true,
            _ => false
        };

    private static async ValueTask HandleExceptionDuringExecution(
        IServiceProvider serviceProvider,
        HttpResponse response,
        ILogger logger,
        ExceptionDispatchInfo error,
        CancellationToken cancellationToken)
    {
        foreach (var handler in serviceProvider.GetServices<IRestExceptionHandler>())
        {
            RestExceptionHandlerResult res;
            try
            {
                res = await handler.HandleAsync(serviceProvider, response, logger, error, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                res = RestExceptionHandlerResult.Unhandled;
            }
            catch (Exception exn)
            {
                LogExceptionHandlerThrownException(logger, exn, handler.GetType());
                res = RestExceptionHandlerResult.Unhandled;
            }
            switch (res)
            {
                case { State: RestExceptionHandlerResult.States.Handled }:
                    LogExceptionHasBeenHandledBy(logger, handler.GetType());
                    return; // stop processing
                case { State: RestExceptionHandlerResult.States.Pass }:
                    LogExceptionHasBeenPassedBy(logger, handler.GetType());
                    error.Throw();
                    return; // never happens
                default:
                    LogExceptionUnhandledBy(logger, handler.GetType());
                    /* noop */
                    break;
            }
        }
        if (StatusCodeResponse.TryExtract(error.SourceException, out var ecode))
        {
            var statusCode = ecode.StatusCode;
            if (response.HasStarted)
            {
                LogExpectedErrorOccuredWhenResponseHasBeenStarted(logger, error.SourceException, statusCode);
            }
            else
            {
                LogExpectedErrorOccured(logger, error.SourceException, statusCode);
                response.StatusCode = statusCode;
                response.Headers.SetCommaSeparatedValues("X-Message", Uri.EscapeDataString(error.SourceException.Message));
            }
        }
        else
        {
            var statusCode = error.SourceException is InvalidOperationException ? 400 : 500;
            if (response.HasStarted)
            {
                LogErrorOccuredWhenResponseHasBeenStarted(logger, error.SourceException);
            }
            else
            {
                LogErrorOccured(logger, error.SourceException);
                response.StatusCode = statusCode;
                response.Headers.SetCommaSeparatedValues("X-Message", Uri.EscapeDataString(error.SourceException.Message));
            }
        }
    }

    private readonly List<Action<EndpointBuilder>> _conventions = [];

    private readonly object _sync = new();

    private readonly RestConfiguration _configuration;

    private readonly IIdParser _idParser;

    private IReadOnlyList<Endpoint>? _endpoints;

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            if (_endpoints is null)
            {
                lock (_sync)
                {
                    _endpoints ??= BuildEndpoints();
                }
            }
            return _endpoints;
        }
    }

    public RestEndpointDataSource(RestConfiguration configuration, IIdParser? idParser = default)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _idParser = idParser ?? DefaultIdParser.Singleton;
    }

    private RouteEndpointBuilder ApplyConventions(RouteEndpointBuilder builder)
    {
        foreach (var convention in _conventions)
        {
            convention(builder);
        }
        return builder;
    }

    private IReadOnlyList<Endpoint> BuildEndpoints()
    {
        var endpoints = new List<Endpoint>();
        var prefixPatternSegment = string.IsNullOrEmpty(_configuration.Prefix) ? default : RoutePatternFactory.Segment(RoutePatternFactory.LiteralPart(_configuration.Prefix));
        var collectionRoutePattern = Segments.Combine(prefixPatternSegment, "type");
        var itemRoutePattern = Segments.Combine(prefixPatternSegment, "type", "id");
        // COMMON
        var idTypeCache = new ConcurrentDictionary<Type, Type>();
        var entitiesConfiguration = _configuration.EntitiesConfiguration;
        var restMetadata = new RestContextMetadata(entitiesConfiguration.Invokers);
        // COLLECTION BASE
        Func<Func<HttpContext, EndpointInvoker, Activity?, Type, Task>, RequestDelegate> restCollectionMethod = implementation =>
            new RequestDelegate(async httpContext =>
            {
                string? entityType = default;
                using var activity = G.ActivitySource.StartActivity("REST method execution", ActivityKind.Server);
                try
                {
                    entityType = (string?)httpContext.Request.RouteValues["type"];
                    if (entityType is not null && entitiesConfiguration.TryResolveType(entityType, out var type))
                    {
                        if (!entitiesConfiguration.Invokers.TryGetValue(type, out var invoker))
                        {
                            throw new InvalidOperationException($"No invoker registered for {type.Name}.");
                        }
                        await implementation(httpContext, invoker, activity, type);
                    }
                    else
                    {
                        httpContext.Response.StatusCode = 404;
                    }
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception exn)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, exn.Message);
                    var error = ExceptionDispatchInfo.Capture(exn);
                    var errorAccessor = httpContext.RequestServices.GetService<IRestErrorAccessor>();
                    if (errorAccessor is not null && errorAccessor is ServiceCollectionRestExtensions.RestErrorAccessor accessor)
                    {
                        accessor.Error = error;
                    }
                    var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger($"NCoreUtils.AspNetCore.Rest.{entityType ?? "Unknown"}");
                    await HandleExceptionDuringExecution(httpContext.RequestServices, httpContext.Response, logger, error, httpContext.RequestAborted);
                }
                finally
                {
                    activity?.Stop();
                }
            });
        // ITEM BASE
        Func<Func<HttpContext, EndpointInvoker, Activity?, Type, object, Task>, RequestDelegate> restItemMethod = implementation =>
            new RequestDelegate(async httpContext =>
            {
                string? entityType = default;
                using var activity = G.ActivitySource.StartActivity("REST method execution", ActivityKind.Server);
                try
                {
                    entityType = (string?)httpContext.Request.RouteValues["type"];
                    if (entityType is not null && entitiesConfiguration.TryResolveType(entityType, out var type))
                    {
                        if (!entitiesConfiguration.Invokers.TryGetValue(type, out var invoker))
                        {
                            throw new InvalidOperationException($"No invoker registered for {type.Name}.");
                        }
                        var idType = invoker.IdType;
                        var id = _idParser.ParseId(httpContext.Request.RouteValues["id"] as string, idType);
                        await implementation(httpContext, invoker, activity, type, id!);
                    }
                    else
                    {
                        httpContext.Response.StatusCode = 404;
                    }
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception exn)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, exn.Message);
                    var error = ExceptionDispatchInfo.Capture(exn);
                    var errorAccessor = httpContext.RequestServices.GetService<IRestErrorAccessor>();
                    if (errorAccessor is not null && errorAccessor is ServiceCollectionRestExtensions.RestErrorAccessor accessor)
                    {
                        accessor.Error = error;
                    }
                    var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger($"NCoreUtils.AspNetCore.Rest.{entityType ?? "Unknown"}");
                    await HandleExceptionDuringExecution(httpContext.RequestServices, httpContext.Response, logger, error, httpContext.RequestAborted);
                }
            });
        var accessConfiguration = _configuration.AccessConfiguration;
        // *********************************************************************************************************
        // COLLECTION ENDPOINT
        RequestDelegate collectionRequestDelegate = restCollectionMethod(
            (httpContext, invoker, activity, entityType) =>
            {
                activity?.SetTag(TagOperation, Operations.Collection);
                return invoker.InvokeList(httpContext, restMetadata, accessConfiguration);
            }
        );
        endpoints.Add(ApplyConventions(new RouteEndpointBuilder(collectionRequestDelegate, collectionRoutePattern, 100)
        {
            DisplayName = "REST-COLLECTION",
            Metadata = { new HttpMethodMetadata([HttpMethods.Get]) }
        }).Build());
        // *********************************************************************************************************
        // ITEM / REDUCTION ENDPOINT
        RequestDelegate itemOrReductionRequestDelegate = restCollectionMethod(
            (httpContext, invoker, activity, entityType) =>
            {
                var arg = (string?)httpContext.Request.RouteValues["id"];
                if (arg is not null && DefaultReductions.Names.Contains(arg))
                {
                    activity?.SetTag(TagOperation, Operations.Reduction);
                    return invoker.InvokeReduction(httpContext, restMetadata, arg, accessConfiguration);
                }
                activity?.SetTag(TagOperation, Operations.Item);
                var idType = invoker.IdType;
                var id = _idParser.ParseId(arg, idType);
                return invoker.InvokeItem(httpContext, restMetadata, id!, accessConfiguration);
            }
        );
        endpoints.Add(ApplyConventions(new RouteEndpointBuilder(itemOrReductionRequestDelegate, itemRoutePattern, 100)
        {
            DisplayName = "REST-ITEM",
            Metadata = { new HttpMethodMetadata([HttpMethods.Get]) }
        }).Build());
        // *********************************************************************************************************
        // CREATE ENDPOINT
        RequestDelegate createRequestDelegate = restCollectionMethod(
            (httpContext, invoker, activity, entityType) =>
            {
                activity?.SetTag(TagOperation, Operations.Create);
                return invoker.InvokeCreate(httpContext, restMetadata, accessConfiguration);
            }
        );
        endpoints.Add(ApplyConventions(new RouteEndpointBuilder(createRequestDelegate, collectionRoutePattern, 100)
        {
            DisplayName = "REST-CREATE",
            Metadata = { new HttpMethodMetadata([HttpMethods.Post]) }
        }).Build());
        // *********************************************************************************************************
        // UPDATE ENDPOINT
        RequestDelegate updateRequestDelegate = restItemMethod(
            (httpContext, invoker, activity, entityType, id) =>
            {
                activity?.SetTag(TagOperation, Operations.Update);
                return invoker.InvokeUpdate(httpContext, restMetadata, id, accessConfiguration);
            }
        );
        endpoints.Add(ApplyConventions(new RouteEndpointBuilder(updateRequestDelegate, itemRoutePattern, 100)
        {
            DisplayName = "REST-UPDATE",
            Metadata = { new HttpMethodMetadata([HttpMethods.Put]) }
        }).Build());
        // *********************************************************************************************************
        // DELETE ENDPOINT
        RequestDelegate deleteRequestDelegate = restItemMethod(
            (httpContext, invoker, activity, entityType, id) =>
            {
                var request = httpContext.Request;
                var force = (request.Headers.TryGetValue("X-Force", out var hvs) && hvs.Any(IsTruthy))
                    || (request.Query.TryGetValue("force", out var qvs) && qvs.Any(IsTruthy));
                activity?.SetTag(TagOperation, Operations.Delete);
                return invoker.InvokeDelete(httpContext, restMetadata, id, force, accessConfiguration);
            }
        );
        endpoints.Add(ApplyConventions(new RouteEndpointBuilder(deleteRequestDelegate, itemRoutePattern, 100)
        {
            DisplayName = "REST-DELETE",
            Metadata = { new HttpMethodMetadata([HttpMethods.Delete]) }
        }).Build());
        // *********************************************************************************************************
        return endpoints;
    }

    public void Add(Action<EndpointBuilder> convention)
        => _conventions.Add(convention);

    public override IChangeToken GetChangeToken()
        => NullChangeToken.Singleton;
}