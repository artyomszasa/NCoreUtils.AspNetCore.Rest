using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public interface IRestJsonSerializerContext
{
    JsonSerializerContext JsonSerializerContext { get; }
}