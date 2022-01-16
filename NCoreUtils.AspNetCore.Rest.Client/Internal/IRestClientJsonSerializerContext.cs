using System.Text.Json.Serialization;

namespace NCoreUtils.Rest.Internal;

public interface IRestClientJsonSerializerContext
{
    JsonSerializerContext JsonSerializerContext { get; }
}