namespace NCoreUtils.AspNetCore.Rest

open System.Collections.Immutable
open System.ComponentModel
open NCoreUtils

/// Defines possible values for ordering direction.
type RestSortByDirection =
  /// Represents ascending direction.
  | Asc  = 0
  /// Represents descending direction.
  | Desc = 1

/// <summary>
/// Represents REST LIST method parameters.
/// </summary>
[<NoEquality; NoComparison>]
type RestQuery = {
  /// Gets index of the first item to be returned in partial query result.
  [<ParameterName("X-Offset")>]
  [<DefaultValue(0)>]
  Offset          : int
  /// Gets number of items to be returned in partial query result.
  [<ParameterName("X-Count")>]
  [<DefaultValue(20)>]
  Count           : int
  /// Gets conditions to be applied when quering items.
  [<ParameterName("X-Filter")>]
  [<DefaultValue(null : string)>]
  Filter          : string
  /// Gets ordering for quering items.
  [<ParameterName("X-Sort-By")>]
  [<ParameterBinder(typeof<DefaultImmutableArrayBinder>)>]
  SortBy          : ImmutableArray<string>
  /// Gets ordering directions for quering items.
  [<ParameterName("X-Sort-By-Direction")>]
  [<ParameterBinder(typeof<DefaultImmutableArrayBinder>)>]
  SortByDirection : ImmutableArray<RestSortByDirection> }