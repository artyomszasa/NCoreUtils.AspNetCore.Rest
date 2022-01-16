using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace NCoreUtils.AspNetCore.Rest
{
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

        private static readonly Func<Type, Type> _idTypeFactory = GetIdType;

        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Exception is thrown if type is not preserved properly.")]
        private static Type GetIdType(Type entityType)
        {
            if (NCoreUtils.Data.IdUtils.TryGetIdType(entityType, out var idType))
            {
                return idType;
            }
            throw new InvalidOperationException($"{entityType} does not implement IHasId interface.");
        }

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

        private readonly List<Action<EndpointBuilder>> _conventions = new List<Action<EndpointBuilder>>();

        private readonly object _sync = new object();

        private readonly RestConfiguration _configuration;

        private IReadOnlyList<Endpoint>? _endpoints;

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                if (_endpoints is null)
                {
                    lock (_sync)
                    {
                        if (_endpoints is null)
                        {
                            _endpoints = BuildEndpoints();
                        }
                    }
                }
                return _endpoints;
            }
        }

        public RestEndpointDataSource(RestConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
            Func<Func<HttpContext, Type, Task>, RequestDelegate> restCollectionMethod = implementation =>
                new RequestDelegate(async httpContext =>
                {
                    string? entityType = default;
                    try
                    {
                        entityType = (string?)httpContext.Request.RouteValues["type"];
                        if (entityType is not null && entitiesConfiguration.TryResolveType(entityType, out var type))
                        {
                            await implementation(httpContext, type);
                        }
                        else
                        {
                            httpContext.Response.StatusCode = 404;
                        }
                    }
                    catch (Exception exn)
                    {
                        var errorAccessor = httpContext.RequestServices.GetService<IRestErrorAccessor>();
                        if (!(errorAccessor is null) && errorAccessor is ServiceCollectionRestExtensions.RestErrorAccessor accessor)
                        {
                            accessor.Error = ExceptionDispatchInfo.Capture(exn);
                        }
                        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger($"NCoreUtils.AspNetCore.Rest.{entityType ?? "Unknown"}");
                        int statusCode;
                        if (StatusCodeResponse.TryExtract(exn, out var ecode))
                        {
                            statusCode = ecode.StatusCode;
                            logger.LogDebug(exn, "Expected error occured during endpoint execution.");
                        }
                        else
                        {
                            statusCode = exn is InvalidOperationException ? 400 : 500;
                            logger.LogError(exn, "Error occured during endpoint execution.");
                        }
                        httpContext.Response.StatusCode = statusCode;
                        httpContext.Response.Headers.Add("X-Message", Uri.EscapeDataString(exn.Message));
                    }
                });
            Func<Func<HttpContext, Type, object, Task>, RequestDelegate> restItemMethod = implementation =>
                new RequestDelegate(async httpContext =>
                {
                    string? entityType = default;
                    try
                    {
                        entityType = (string?)httpContext.Request.RouteValues["type"];
                        if (entityType is not null && entitiesConfiguration.TryResolveType(entityType, out var type))
                        {
                            var idType = idTypeCache.GetOrAdd(type, _idTypeFactory);
                            await implementation(httpContext, type, Convert.ChangeType(httpContext.Request.RouteValues["id"], idType)!);
                        }
                        else
                        {
                            httpContext.Response.StatusCode = 404;
                        }
                    }
                    catch (Exception exn)
                    {
                        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger($"NCoreUtils.AspNetCore.Rest.{entityType ?? "Unknown"}");
                        int statusCode;
                        if (StatusCodeResponse.TryExtract(exn, out var ecode))
                        {
                            statusCode = ecode.StatusCode;
                            logger.LogDebug(exn, "Expected error occured during endpoint execution.");
                        }
                        else
                        {
                            statusCode = exn is InvalidOperationException ? 400 : 500;
                            logger.LogError(exn, "Error occured during endpoint execution.");
                        }
                        httpContext.Response.StatusCode = statusCode;
                        httpContext.Response.Headers.Add("X-Message", exn.Message);
                    }
                });
            var accessConfiguration = _configuration.AccessConfiguration;
            // *********************************************************************************************************
            // COLLECTION ENDPOINT
            RequestDelegate collectionRequestDelegate = restCollectionMethod(
                (httpContext, entityType) => Invoker.InvokeList(entityType, httpContext, accessConfiguration)
            );
            endpoints.Add(ApplyConventions(new RouteEndpointBuilder(collectionRequestDelegate, collectionRoutePattern, 100)
            {
                DisplayName = $"REST-COLLECTION",
                Metadata = { new HttpMethodMetadata(new [] { "GET" }) }
            }).Build());
            // *********************************************************************************************************
            // ITEM / REDUCTION ENDPOINT
            RequestDelegate itemRequestDelegate = restCollectionMethod(
                (httpContext, entityType) =>
                {
                    var arg = (string?)httpContext.Request.RouteValues["id"];
                    if (arg is not null && DefaultReductions.Names.Contains(arg))
                    {
                        return Invoker.InvokeReduction(entityType, httpContext, arg, accessConfiguration);
                    }
                    var idType = idTypeCache.GetOrAdd(entityType, _idTypeFactory);
                    return Invoker.InvokeItem(entityType, httpContext, Convert.ChangeType(arg, idType)!, accessConfiguration);
                }
            );
            endpoints.Add(ApplyConventions(new RouteEndpointBuilder(itemRequestDelegate, itemRoutePattern, 100)
            {
                DisplayName = $"REST-ITEM",
                Metadata = { new HttpMethodMetadata(new [] { "GET" }) }
            }).Build());
            // *********************************************************************************************************
            // CREATE ENDPOINT
            RequestDelegate createRequestDelegate = restCollectionMethod(
                (httpContext, entityType) => Invoker.InvokeCreate(entityType, httpContext, accessConfiguration)
            );
            endpoints.Add(ApplyConventions(new RouteEndpointBuilder(createRequestDelegate, collectionRoutePattern, 100)
            {
                DisplayName = $"REST-CREATE",
                Metadata = { new HttpMethodMetadata(new [] { "POST" }) }
            }).Build());
            // *********************************************************************************************************
            // UPDATE ENDPOINT
            RequestDelegate updateRequestDelegate = restItemMethod(
                (httpContext, entityType, id) => Invoker.InvokeUpdate(entityType, httpContext, id, accessConfiguration)
            );
            endpoints.Add(ApplyConventions(new RouteEndpointBuilder(updateRequestDelegate, itemRoutePattern, 100)
            {
                DisplayName = $"REST-UPDATE",
                Metadata = { new HttpMethodMetadata(new [] { "PUT" }) }
            }).Build());
            // *********************************************************************************************************
            // DELETE ENDPOINT
            RequestDelegate deleteRequestDelegate = restItemMethod(
                (httpContext, entityType, id) =>
                {
                    var request = httpContext.Request;
                    var force = (request.Headers.TryGetValue("X-Force", out var hvs) && hvs.Any(IsTruthy))
                        || (request.Query.TryGetValue("force", out var qvs) && qvs.Any(IsTruthy));
                    return Invoker.InvokeDelete(entityType, httpContext, id, force, accessConfiguration);
                }
            );
            endpoints.Add(ApplyConventions(new RouteEndpointBuilder(deleteRequestDelegate, itemRoutePattern, 100)
            {
                DisplayName = $"REST-DELETE",
                Metadata = { new HttpMethodMetadata(new [] { "DELETE" }) }
            }).Build());
            // *********************************************************************************************************
            return endpoints;
        }

        public void Add(Action<EndpointBuilder> convention)
            => _conventions.Add(convention);

        public override IChangeToken GetChangeToken()
            => NullChangeToken.Singleton;
    }
}