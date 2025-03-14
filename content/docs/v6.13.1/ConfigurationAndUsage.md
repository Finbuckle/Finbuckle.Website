# Configuration and Usage

## Configuration

Finbuckle.MultiTenant uses the standard application builder pattern for its configuration. In addition to adding the
services, configuration for one or more [MultiTenant Stores](Stores) and [MultiTenant Strategies](Strategies) are
required:

```csharp
using Finbuckle.MultiTenant;

var builder = WebApplication.CreateBuilder(args);

// ...add app services

// add Finbuckle.MultiTenant services
builder.Services.AddMultiTenant<TenantInfo>()
    .WithHostStrategy()
    .WithConfigurationStore();

var app = builder.Build();

// add the Finbuckle.MultiTenant middleware
app.UseMultiTenant();

// ...add other middleware

app.Run();
```

## Adding the Finbuckle.MultiTenant Service

Use the `AddMultiTenant<TTenantInfo>` extension method on `IServiceCollection` to register the basic dependencies
needed by the library. It returns a `MultiTenantBuilder<TTenantInfo>` instance on which the methods below can be called
for further configuration. Each of these methods returns the same `MultiTenantBuilder<TTenantInfo>` instance allowing
for chaining method calls.

## Configuring the Service

### WithStore Variants

Adds and configures an IMultiTenantStore to the application. Only the last store configured will be used.
See [MultiTenant Stores](Stores) for more information on each type.

- `WithStore<TStore>`
- `WithInMemoryStore<TTenantStore>`
- `WithConfigurationStore<TTenantStore>`
- `WithEFCoreStore<TTenantStore>`
- `WithDistributedCacheStore<TTenantStore>`
- `WithHttpRemoteStore<TTenantStore>`

### WithStrategy Variants

Adds and configures an IMultiTenantStore to the application. Multiple strategies can be configured and each will be used
in the order registered. See [MultiTenant Strategies](Strategies) for more information on each type.

- `WithStrategy<TStrategy>`
- `WithBasePathStrategy`
- `WithClaimStrategy`
- `WithDelegateStrategy`
- `WithHeaderStrategy`
- `WithHostStrategy`
- `WithRouteStrategy`
- `WithSessionStrategy`
- `WithStaticStrategy`

### WithPerTenantAuthentication

Configures support for per-tenant authentication. See [Per-Tenant Authentication](Authentication) for more details.

## Per-Tenant Options

Finbuckle.MultiTenant integrates with the
standard [.NET Options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) (see also the [ASP.NET
Core Options pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)) and lets apps
customize options distinctly for each tenant. See [Per-Tenant Options](Options) for more details.

## Tenant Resolution

Most of the capability enabled by Finbuckle.MultiTenant is utilized through its middleware and use
the [Options pattern with per-tenant options](Options). For web applications the middleware will resolve the app's
current tenant on each request using the configured strategies and stores, and the per-tenant
options will alter the app's behavior as dependency injection passes the options to app components.

## Getting the Current Tenant

There are several ways an app can see the current tenant:

### Dependency Injection

* By default Finbuckle.MultiTenant registers the `ITenantInfo` and your custom `TTenantInfo` for dependency injection.
  However, be aware that these can be `null` if there is no current tenant.

* `IMultiTenantContext` and `IMultiTenantContext<TTenantInfo>` are also available for depency injection. These will
  never
  be `null` although there may not be a valid current tenant.

* `IMultiTenantContextAccessor` and `IMultiContextContextAccessor` are also available via dependency injection for
  advanced scenarios.

### `HttpContext` Extension Methods

For web apps these convenience methods are also available:

* `GetMultiTenantContext<TTenantInfo>`

  Use this `HttpContext` extension method to get the `MultiTenantContext<TTenantInfo>` instance for the current
  request.

  ```csharp
  var tenantInfo = HttpContext.GetMultiTenantContext<TenantInfo>().TenantInfo;
  
  if(tenantInfo != null)
  {
    var tenantId = tenantInfo.Id;
    var identifier = tenantInfo.Identifier;
    var name = tenantInfo.Name;
    var something = tenantInfo.Items["something"];
  }
  ```

* `TrySetTenantInfo<TTenantInfo>`

  For most cases the middleware sets the `TenantInfo` and this method is not needed. Use only if explicitly
  overriding the `TenantInfo` set by the middleware.

  Use this 'HttpContext' extension method to the current tenant to the provided `TenantInfo`. Returns true if
  successful. Optionally it can also reset the service provider scope so that any scoped services already resolved will
  be
  resolved again under the current tenant when needed. This has no effect on singleton or transient services. Setting
  the `TenantInfo` with this method sets both the `StoreInfo` and `StrategyInfo` properties on
  the `MultiTenantContext<TTenantInfo>` to `null`.

  ```csharp
  var newTenantInfo = new TenantInfo(...);
  
  if(HttpContext.TrySetTenantInfo(newTenantInfo, resetServiceProvider: true))
  {
      // This will be the new tenant.
      var tenant = HttpContext.GetMultiTenantContext().TenantInfo;
  
      // This will regenerate the options class.
      var optionsProvider = HttpContext.RequestServices.GetService<IOptions<MyScopedOptions>>();
  }
  ```
