[<AutoOpen>]
module internal NCoreUtils.AspNetCore.Rest.PropertyInfoExt

open System
open System.Reflection
open System.Threading
open NCoreUtils
open System.Linq.Expressions

let mutable private idSupply = 0L

type PropertyInfo with
  member this.CreateSelector (?parameterType : Type) =
    let eArgType =
      match parameterType with
      | Some ptype when not (isNull ptype) ->
        if not (this.DeclaringType.IsAssignableFrom ptype) then
          invalidOpf "%s cannot be used as parameter type for property selector of %s.%s." ptype.FullName this.DeclaringType.FullName this.Name
        ptype
      | _ -> this.DeclaringType
    let eArg = Expression.Parameter (eArgType, sprintf "__propertySelectorArg%d" (Interlocked.Increment (&idSupply)))
    Expression.Lambda (Expression.Property (eArg, this), [| eArg |])
