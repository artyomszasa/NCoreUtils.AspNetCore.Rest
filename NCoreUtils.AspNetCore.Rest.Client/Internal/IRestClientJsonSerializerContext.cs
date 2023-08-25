using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.Rest.Internal;

[Obsolete("Use IRestClientJsonTypeInfoResolver")]
public interface IRestClientJsonSerializerContext
{
    JsonSerializerContext JsonSerializerContext { get; }
}