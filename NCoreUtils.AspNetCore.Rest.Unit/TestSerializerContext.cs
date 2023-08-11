using System.Collections.Generic;
using System.Text.Json.Serialization;
using NCoreUtils.AspNetCore.Rest.Unit.Data;

namespace NCoreUtils.AspNetCore.Rest.Unit;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(IAsyncEnumerable<TestData>))]
public partial class TestSerializerContext : JsonSerializerContext { }