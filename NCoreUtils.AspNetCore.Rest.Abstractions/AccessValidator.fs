namespace NCoreUtils.AspNetCore.Rest

open System
open System.Linq
open System.Security.Claims
open System.Threading
open System.Threading.Tasks
open NCoreUtils
open System.Runtime.CompilerServices

/// <summary>
/// Defines functionality to implement generic access validation.
/// </summary>
[<AllowNullLiteral>]
[<Interface>]
type IAccessValidator =
  /// <summary>
  /// Returns whether operation is accessible with respect to the specified parameters.
  /// </summary>
  /// <param name="serviceProvider">Service provider of the current context.</param>
  /// <param name="principal">Principal information for the current context.</param>
  /// <returns><c>true</c> if operation can be performed, otherwise <c>false</c></returns>
  abstract AsyncValidate : serviceProvider:IServiceProvider * principal:ClaimsPrincipal -> Async<bool>

/// <summary>
/// Defines functionality to implement generic access validation for single entity.
/// </summary>
[<AllowNullLiteral>]
[<Interface>]
type IEntityAccessValidator =
  inherit IAccessValidator
  /// <summary>
  /// Returns whether operation is accessible on the specified entity with respect to the specified parameters.
  /// </summary>
  /// <param name="entity">Target entity.</param>
  /// <param name="serviceProvider">Service provider of the current context.</param>
  /// <param name="principal">Principal information for the current context.</param>
  /// <returns><c>true</c> if operation can be performed, otherwise <c>false</c></returns>
  abstract AsyncValidate : entity:obj * serviceProvider:IServiceProvider * principal:ClaimsPrincipal -> Async<bool>

/// <summary>
/// Defines functionality to implement generic access validation for queryable.
/// </summary>
[<AllowNullLiteral>]
[<Interface>]
type IQueryAccessValidator =
  inherit IAccessValidator
  /// <summary>
  /// Decorates specified queryable to return only accessible items.
  /// </summary>
  /// <param name="queryable">Source queryable.</param>
  /// <param name="serviceProvider">Service provider of the current context.</param>
  /// <param name="principal">Principal information for the current context.</param>
  /// <returns>Decorated queryable.</returns>
  abstract AsyncFilterQuery : queryable:IQueryable * serviceProvider:IServiceProvider * principal:ClaimsPrincipal -> Async<IQueryable>

/// <summary>
/// Defines functionality to implement generic access validation that will be instantiated using dependency injection
/// container.
/// </summary>
type IAccessValidation =
  /// <summary>
  /// Returns whether operation is accessible with respect to the specified parameters.
  /// </summary>
  /// <param name="principal">Principal information for the current context.</param>
  /// <returns><c>true</c> if operation can be performed, otherwise <c>false</c></returns>
  abstract AsyncValidate : principal:ClaimsPrincipal -> Async<bool>

/// <summary>
/// Defines functionality to implement generic access validation for single entity that will be instantiated using
/// dependency injection container.
/// </summary>
type IEntityAccessValidation =
  inherit IAccessValidation
  /// <summary>
  /// Returns whether operation is accessible on the specified entity with respect to the specified parameters.
  /// </summary>
  /// <param name="entity">Target entity.</param>
  /// <param name="principal">Principal information for the current context.</param>
  /// <returns><c>true</c> if operation can be performed, otherwise <c>false</c></returns>
  abstract AsyncValidate : entity:obj * principal:ClaimsPrincipal -> Async<bool>

/// <summary>
/// Defines functionality to implement generic access validation for queryable that will be instantiated using
/// dependency injection container.
/// </summary>
type IQueryAccessValidation =
  inherit IAccessValidation
  /// <summary>
  /// Decorates specified queryable to return only accessible items.
  /// </summary>
  /// <param name="queryable">Source queryable.</param>
  /// <param name="principal">Principal information for the current context.</param>
  /// <returns>Decorated queryable.</returns>
  abstract AsyncFilterQuery : queryable:IQueryable * principal:ClaimsPrincipal -> Async<IQueryable>

// C# interop

/// <summary>
/// Represents generic access validation that will be instantiated using dependency injection container.
/// </summary>
[<AbstractClass>]
type AsyncAccessValidation () =
  /// <summary>
  /// Returns whether operation is accessible with respect to the specified parameters.
  /// </summary>
  /// <param name="principal">Principal information for the current context.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns><c>true</c> if operation can be performed, otherwise <c>false</c></returns>
  abstract ValidateAsync : principal:ClaimsPrincipal * cancellationToken:CancellationToken -> Task<bool>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.ValidateDirect (principal, cancellationToken) = this.ValidateAsync (principal, cancellationToken)
  interface IAccessValidation with
    member this.AsyncValidate principal = Async.Adapt (fun cancellationToken -> this.ValidateAsync (principal, cancellationToken))

/// <summary>
/// Represents generic access validation for single entity that will be instantiated using dependency injection
/// container.
/// </summary>
[<AbstractClass>]
type AsyncEntityAccessValidation () =
  inherit AsyncAccessValidation ()
  /// <summary>
  /// Returns whether operation is accessible on the specified entity with respect to the specified parameters.
  /// </summary>
  /// <param name="entity">Target entity.</param>
  /// <param name="principal">Principal information for the current context.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns><c>true</c> if operation can be performed, otherwise <c>false</c></returns>
  abstract ValidateAsync : entity:obj * principal:ClaimsPrincipal * cancellationToken:CancellationToken -> Task<bool>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.ValidateDirect (entity, principal, cancellationToken) = this.ValidateAsync (entity, principal, cancellationToken)
  interface IEntityAccessValidation with
    member this.AsyncValidate (entity, principal) = Async.Adapt (fun cancellationToken -> this.ValidateAsync (entity, principal, cancellationToken))

/// <summary>
/// Represents generic access validation for queryable that will be instantiated using dependency injection container.
/// </summary>
[<AbstractClass>]
type AsyncQueryAccessValidation () =
  inherit AsyncAccessValidation ()
  /// <summary>
  /// Decorates specified queryable to return only accessible items.
  /// </summary>
  /// <param name="queryable">Source queryable.</param>
  /// <param name="principal">Principal information for the current context.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Decorated queryable.</returns>
  abstract FilterQueryAsync : queryable:IQueryable * principal:ClaimsPrincipal * cancellationToken:CancellationToken -> Task<IQueryable>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.FilterQueryDirect (queryable : IQueryable, principal, cancellationToken) = this.FilterQueryAsync (queryable, principal, cancellationToken)
  interface IQueryAccessValidation with
    member this.AsyncFilterQuery (queryable, principal) = Async.Adapt (fun cancellationToken -> this.FilterQueryAsync (queryable, principal, cancellationToken))

/// Contains extensions for access validation.
[<AbstractClass; Sealed>]
[<Extension>]
type AccessValidationExtensions =

  /// <summary>
  /// Returns whether operation is accessible with respect to the specified parameters.
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="principal">Principal information for the current context.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns><c>true</c> if operation can be performed, otherwise <c>false</c></returns>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ValidateAsync (this : IAccessValidation, principal : ClaimsPrincipal, cancellationToken) =
    match this with
    | :? AsyncAccessValidation as inst -> inst.ValidateDirect (principal, cancellationToken)
    | _                                -> Async.StartAsTask (this.AsyncValidate principal, cancellationToken = cancellationToken)

  /// <summary>
  /// Returns whether operation is accessible on the specified entity with respect to the specified parameters.
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="entity">Target entity.</param>
  /// <param name="principal">Principal information for the current context.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns><c>true</c> if operation can be performed, otherwise <c>false</c></returns>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ValidateAsync (this : IEntityAccessValidation, entity, principal : ClaimsPrincipal, cancellationToken) =
    match this with
    | :? AsyncEntityAccessValidation as inst -> inst.ValidateDirect (entity, principal, cancellationToken)
    | _                                      -> Async.StartAsTask (this.AsyncValidate (entity, principal), cancellationToken = cancellationToken)

  /// <summary>
  /// Decorates specified queryable to return only accessible items.
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="queryable">Source queryable.</param>
  /// <param name="principal">Principal information for the current context.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Decorated queryable.</returns>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member FilterQueryAsync (this : IQueryAccessValidation, queryable, principal : ClaimsPrincipal, cancellationToken) =
    match this with
    | :? AsyncQueryAccessValidation as inst -> inst.FilterQueryDirect (queryable, principal, cancellationToken)
    | _                                     -> Async.StartAsTask (this.AsyncFilterQuery (queryable, principal), cancellationToken = cancellationToken)

