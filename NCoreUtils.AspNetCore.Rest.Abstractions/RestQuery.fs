namespace NCoreUtils.AspNetCore.Rest

open System.Collections.Immutable
open System.ComponentModel
open NCoreUtils

type RestSortByDirection =
  | Asc  = 0
  | Desc = 1

[<NoEquality; NoComparison>]
type RestQuery = {
  [<ParameterName("X-Offset")>]
  [<DefaultValue(0)>]
  Offset          : int
  [<ParameterName("X-Count")>]
  [<DefaultValue(20)>]
  Count           : int
  [<ParameterName("X-Filter")>]
  [<DefaultValue(null : string)>]
  Filter          : string
  [<ParameterName("X-Sort-By")>]
  [<ParameterBinder(typeof<DefaultImmutableArrayBinder>)>]
  SortBy          : ImmutableArray<string>
  [<ParameterName("X-Sort-By-Direction")>]
  [<ParameterBinder(typeof<DefaultImmutableArrayBinder>)>]
  SortByDirection : ImmutableArray<RestSortByDirection> }