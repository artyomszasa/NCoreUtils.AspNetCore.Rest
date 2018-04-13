namespace NCoreUtils.AspNetCore.Rest

open System

/// Bound services for REST methods.
type RestMethodServices = {
  /// Local service provider to be used to resolve REST services.
  ServiceProvider   : IServiceProvider
  /// Holds unresolved type name of the entity being accessed.
  CurrentTypeName   : CurrentRestTypeName
  /// REST configuration.
  RestConfiguration : RestConfiguration }
