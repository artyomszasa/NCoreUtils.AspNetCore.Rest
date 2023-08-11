using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Internal;

[Obsolete("Use IRestJsonTypeInfoResolver instead.")]
public interface IRestJsonSerializerContext
{
    JsonSerializerContext JsonSerializerContext { get; }
}