using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Provides default object deserializer based on <c>System.Text.Json</c>.
    /// </summary>
    /// <typeparam name="T">Target object type.</typeparam>
    public class HttpContextAwareDeserializer<T> : IDeserializer<T>
    {
        static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

        static readonly JsonSerializerOptions _defaultJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Eqi(string a, string b) => StringComparer.OrdinalIgnoreCase.Equals(a, b);

        readonly IHttpContextAccessor _httpContextAccessor;

        readonly ILogger _logger;


        /// <summary>
        /// Initializes new instance from the specified parameters.
        /// </summary>
        /// <param name="httpContextAccessor">Http context accessor to use.</param>
        /// <param name="logger">Logger to use.</param>
        public HttpContextAwareDeserializer(IHttpContextAccessor httpContextAccessor, ILogger<HttpContextAwareDeserializer<T>> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Deserializes object of the specified type from json input.
        /// </summary>
        /// <param name="stream">Stream to deserialize from.</param>
        /// <param name="contentType">Content type information.</param>
        /// <param name="jsonOptions">Resolved json serialization options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Deserialized object.</returns>
        protected virtual async ValueTask<T> DeserializeJsonAsync(
            Stream stream,
            MediaTypeHeaderValue contentType,
            JsonSerializerOptions jsonOptions,
            CancellationToken cancellationToken)
        {
            T data;
            if (contentType.Charset.HasValue && !Eqi("utf-8", contentType.Charset.Value))
            {
                // read body using specific encoding and parse aas string
                string buffer;
                using var reader = new StreamReader(stream, contentType.Encoding, false);
                buffer = await reader.ReadToEndAsync();
                data = JsonSerializer.Deserialize<T>(buffer, jsonOptions);
            }
            else
            {
                data = await JsonSerializer.DeserializeAsync<T>(stream, jsonOptions, cancellationToken);
            }
            return data;
        }

        /// <summary>
        /// Deserializes object of the specified type with respect to the Content-Type header of the implicit HttpContext.
        /// </summary>
        /// <param name="stream">Stream to deserialize from.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Deserialized object.</returns>
        public virtual async ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException($"Trying to deserialize entity without context.");
            var request = context.Request ?? throw new InvalidOperationException($"Trying to deserialize entity without context.");
            var headers = request.GetTypedHeaders();
            switch (headers.ContentType)
            {
                case null:
                    throw new UnsupportedMediaTypeException("Unable to deserialize entity as no content type has been specified in request.");
                case MediaTypeHeaderValue contentType:
                    if (contentType.MediaType.HasValue)
                    {
                        throw new UnsupportedMediaTypeException("Unable to deserialize entity as no media type has been specified in request.");
                    }
                    if (Eqi("application/json", contentType.MediaType.Value) || Eqi("text/json", contentType.MediaType.Value))
                    {
                        var jsonOptions = context.RequestServices
                            .GetOptionalService<JsonSerializerOptions>()
                            ?? _defaultJsonOptions;
                        try
                        {
                            var data = await DeserializeJsonAsync(stream, contentType, jsonOptions, cancellationToken);
                            _logger.LogDebug("Sucessfully deserialized request body as {0}.", typeof(T));
                            return data;
                        }
                        catch (Exception exn)
                        {
                            throw new BadRequestException($"Unable to deserialize request body as {typeof(T)}.", exn);
                        }
                    }
                    throw new UnsupportedMediaTypeException();
            }
        }
    }
}