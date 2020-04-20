using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    public class DefaultSerializer<T> : ISerializer<T>
    {
        readonly JsonSerializerOptions _jsonOptions;

        public DefaultSerializer(JsonSerializerOptions jsonOptions)
        {
            _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
        }

        public async ValueTask SerializeAsync(IConfigurableOutput<Stream> configurableStream, T item, CancellationToken cancellationToken)
        {
            using var stream = await configurableStream.InitializeAsync(new OutputInfo(default, "application/json; charset=utf-8"), cancellationToken);
            await JsonSerializer.SerializeAsync(stream, item, _jsonOptions, cancellationToken);
        }
    }
}