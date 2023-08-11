using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore.Rest.Serialization;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public abstract class CreateInvoker
    {
        protected sealed class RestCreateInvocation<TData, TId> : RestMethodInvocation<TData>
            where TData : notnull, IHasId<TId>
        {
            readonly IRestCreate<TData, TId> _invoker;

            readonly ArgumentCollection<TData> _args;

            public override Type ItemType => typeof(TData);

            public override object Instance => _invoker;

            public override IReadOnlyList<object> Arguments => _args;

            public RestCreateInvocation(IRestCreate<TData, TId> invoker, TData data)
            {
                _invoker = invoker;
                _args = new ArgumentCollection<TData>(data);
            }

            public override ValueTask<TData> InvokeAsync(CancellationToken cancellationToken = default)
                => _invoker.InvokeAsync(_args.Arg, cancellationToken);

            public override RestMethodInvocation<TData> UpdateArguments(IReadOnlyList<object> arguments)
            {
                if (arguments.Count != 1)
                {
                    throw new InvalidOperationException("Invalid number of arguments.");
                }
                return new RestCreateInvocation<TData, TId>(_invoker, (TData)arguments[0]);
            }
        }

        internal CreateInvoker() { }

        public abstract ValueTask Invoke(HttpContext httpContext, CancellationToken cancellationToken);
    }

    public sealed class CreateInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId> : CreateInvoker
        where TData : notnull, IHasId<TId>
    {
        readonly IServiceProvider _serviceProvider;

        readonly RestAccessConfiguration _accessConfiguration;

        readonly IRestMethodInvoker _methodInvoker;

        readonly IRestCreate<TData, TId> _implementation;

        readonly IDeserializer<TData> _deserializer;

        public CreateInvoker(
            IServiceProvider serviceProvider,
            RestAccessConfiguration accessConfiguration,
            IRestMethodInvoker? methodInvoker = default,
            IRestCreate<TData, TId>? implementation = default)
        {
            _serviceProvider = serviceProvider;
            _accessConfiguration = accessConfiguration;
            _methodInvoker = methodInvoker ?? DefaultRestMethodInvoker.Instance;
            _implementation = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestCreate<TData, TId>>(serviceProvider);
            _deserializer = serviceProvider.GetOrCreateDeserializer<TData>();
        }

        private Uri CreateItemUri(HttpContext httpContext, TId id)
        {
            // TODO: generate location based on configuration instead...
            var request = httpContext.Request;
            var builder = new UriBuilder
            {
                Scheme = request.Scheme,
                Path = request.Path.ToUriComponent()
            };
            if (request.Host.HasValue)
            {
                builder.Host = request.Host.Host;
                if (request.Host.Port.HasValue)
                {
                    var port = request.Host.Port.Value;
                    if (!((request.IsHttps && port == 443) || (!request.IsHttps && port == 80)))
                    {
                        builder.Port = port;
                    }
                }
            }
            else
            {
                builder.Host = "127.0.0.1";
            }
            var idPart = id is IConvertible convertible
                ? convertible.ToString(CultureInfo.InvariantCulture)
                : id!.ToString();
            if (builder.Path.EndsWith('/'))
            {
                builder.Path += idPart;
            }
            else
            {
                builder.Path += $"/{idPart}";
            }
            return builder.Uri;
        }

        public override async ValueTask Invoke(HttpContext httpContext, CancellationToken cancellationToken)
        {
            var accessValidator = _accessConfiguration.Create.GetOrCreateValidator(_serviceProvider, out var disposeValidator);
            try
            {
                (await accessValidator.ValidateAsync(httpContext.User, cancellationToken)).ThrowOnFailure();
                var data = await _deserializer.DeserializeAsync(httpContext.Request.Body, cancellationToken);
                var invocation = new RestCreateInvocation<TData, TId>(_implementation, data);
                var result = await _methodInvoker.InvokeAsync(invocation, cancellationToken);
                httpContext.Response.Headers.Append("Location", CreateItemUri(httpContext, result.Id).AbsoluteUri);
                httpContext.Response.StatusCode = 201;
            }
            finally
            {
                if (disposeValidator)
                {
                    (accessValidator as IDisposable)?.Dispose();
                }
            }
        }
    }
}