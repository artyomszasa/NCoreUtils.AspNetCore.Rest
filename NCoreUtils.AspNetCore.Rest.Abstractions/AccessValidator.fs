namespace NCoreUtils.AspNetCore.Rest

open System
open System.Linq
open System.Security.Claims
open System.Threading
open System.Threading.Tasks
open NCoreUtils
open System.Runtime.CompilerServices

[<AllowNullLiteral>]
type IAccessValidator =
  abstract AsyncValidate : serviceProvider:IServiceProvider * principal:ClaimsPrincipal -> Async<bool>

[<AllowNullLiteral>]
type IEntityAccessValidator =
  inherit IAccessValidator
  abstract AsyncValidate : entity:obj * serviceProvider:IServiceProvider * principal:ClaimsPrincipal -> Async<bool>

[<AllowNullLiteral>]
type IQueryAccessValidator =
  inherit IAccessValidator
  abstract AsyncFilterQuery : queryable:IQueryable * serviceProvider:IServiceProvider * principal:ClaimsPrincipal -> Async<IQueryable>


type IAccessValidation =
  abstract AsyncValidate : principal:ClaimsPrincipal -> Async<bool>

type IEntityAccessValidation =
  inherit IAccessValidation
  abstract AsyncValidate : entity:obj * principal:ClaimsPrincipal -> Async<bool>

type IQueryAccessValidation =
  inherit IAccessValidation
  abstract AsyncFilterQuery : queryable:IQueryable * principal:ClaimsPrincipal -> Async<IQueryable>

// C# interop

[<AbstractClass>]
type AsyncAccessValidation () =
  abstract ValidateAsync : principal:ClaimsPrincipal * cancellationToken:CancellationToken -> Task<bool>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.ValidateDirect (principal, cancellationToken) = this.ValidateAsync (principal, cancellationToken)
  interface IAccessValidation with
    member this.AsyncValidate principal = Async.Adapt (fun cancellationToken -> this.ValidateAsync (principal, cancellationToken))

[<AbstractClass>]
type AsyncEntityAccessValidation () =
  inherit AsyncAccessValidation ()
  abstract ValidateAsync : entity:obj * principal:ClaimsPrincipal * cancellationToken:CancellationToken -> Task<bool>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.ValidateDirect (entity, principal, cancellationToken) = this.ValidateAsync (entity, principal, cancellationToken)
  interface IEntityAccessValidation with
    member this.AsyncValidate (entity, principal) = Async.Adapt (fun cancellationToken -> this.ValidateAsync (entity, principal, cancellationToken))

[<AbstractClass>]
type AsyncQueryAccessValidation () =
  inherit AsyncAccessValidation ()
  abstract FilterQueryAsync : queryable:IQueryable * principal:ClaimsPrincipal * cancellationToken:CancellationToken -> Task<IQueryable>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.FilterQueryDirect (queryable : IQueryable, principal, cancellationToken) = this.FilterQueryAsync (queryable, principal, cancellationToken)
  interface IQueryAccessValidation with
    member this.AsyncFilterQuery (queryable, principal) = Async.Adapt (fun cancellationToken -> this.FilterQueryAsync (queryable, principal, cancellationToken))

[<AbstractClass; Sealed>]
[<Extension>]
type AccessValidationExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ValidateAsync (this : IAccessValidation, principal : ClaimsPrincipal, cancellationToken) =
    match this with
    | :? AsyncAccessValidation as inst -> inst.ValidateDirect (principal, cancellationToken)
    | _                                -> Async.StartAsTask (this.AsyncValidate principal, cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ValidateAsync (this : IEntityAccessValidation, entity, principal : ClaimsPrincipal, cancellationToken) =
    match this with
    | :? AsyncEntityAccessValidation as inst -> inst.ValidateDirect (entity, principal, cancellationToken)
    | _                                      -> Async.StartAsTask (this.AsyncValidate (entity, principal), cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member FilterQueryAsync (this : IQueryAccessValidation, queryable, principal : ClaimsPrincipal, cancellationToken) =
    match this with
    | :? AsyncQueryAccessValidation as inst -> inst.FilterQueryDirect (queryable, principal, cancellationToken)
    | _                                     -> Async.StartAsTask (this.AsyncFilterQuery (queryable, principal), cancellationToken = cancellationToken)

