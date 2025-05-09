# Per-Tenant Authentication

Finbuckle.MultiTenant provides built-in support for isolating tenant
authentication. This means that the login session for a given request will only
be valid for the current tenant. Subsequent requests from the same client, but
for a different tenant (e.g. a different path when using the route strategy),
will not leak the previous login session into the tenant. This feature also
avoids the need to create separate authentication schemes for each tenant.

Common authentication options are supported per-tenant as discussed below, but
additional authentication options can be configured per-tenant using
[per-tenant options](Options) as needed.


The sections below assume Finbuckle.MultiTenant is installed and configured. See
[Getting Started](GettingStarted) for details.

## Using WithPerTenantAuthentication()

The `WithPerTenantAuthentication()` method can be called after
`AddMultiTenant<TTenantInfo>()` and uses conventions to configure common authentication
options based on public properties of the `ITenantInfo` type parameter.

The following happens when `WithPerTenantAuthentication()` is called:

- Cookie sign-in events are modified to add a tenant claim during sign-in. Existing
  sign-in events are preserved.
- Cookie validation events are modified to validate that a tenant claim exists
  which matches the current requests tenant. Existing validation events are
  preserved.

The following also happens if the `ItenantInfo` implementation has the appropriate property:

- The default challenge scheme is set to the `ChallengeScheme` property
  of the `ITenantInfo` implementation.
- 'LoginPath' for cookie authentication is set to the `CookieLoginPath` property
  of the `ITenantInfo` implementation.
- 'LogoutPath' for cookie authentication is set to the `CookieLogoutPath`
  property of the `ITenantInfo` implementation.
- 'AccessDeniedPath' for cookie authentication is set to the
  `CookieAccessDeniedPath` property of the `ITenantInfo` implementation.
- Several internal services are registered to support remote authentication such
  as OAuth 2.0 and OpenID Connect.
- `Authority` for OpenID connect authentication is set to the
  `OpenIdConnectAuthority` property of the `ITenantInfo` implementation.
- `ClientId` for OpenID connect authentication is set to the
  `OpenIdConnectClientId` property of the `ITenantInfo` implementation.
- `ClientSecret` for OpenID connect authentication is set to the
  `OpenIdConnectClientSecret` property of the `ITenantInfo` implementation.

If the `ITenantInfo` implementation lacks one of these properties there is no
impact on the respective authentication property.

The cookie sign-in and validation events ensure that a tenant sign-in does not
leak over to a request for another tenant within the same browser or agent. By
default, if a new sign-in occurs under a new tenant then tenant claim is replaced
and the prior tenant session is effectively signed off. Any request to the prior
tenant will lack the correct tenant claim value and validation will reject the
authentication. This behavior means only a single tenant sign-in can be active.
See [other authentication options](#other-authentication-options) below if a
separate sign-in cookie for each tenant is required.

By changing the default challenge per-tenant, the user can be redirected to a
different scheme as needed. Combined with a per-tenant OpenID Connect authority,
this can route to shared or tenant specific authentication infrastructure.

The `CookieLoginPath`, `CookieLogoutPath`, `CookieAccessDeniedPath`, `OpenIdConnectAuthority`, `OpenIdConnectClientId`
, `OpenIdConnectClientSecret` properties can use a template format where `__tenant__`
will be replaced with the identifier for each specific tenant. For example, a
`CookieLoginPath` of "/\_\_tenant\_\_/Identity/Account/Login" will result in
"/initech/Identity/Account/Login" for the Initech tenant.

The code setup is straight-forward:

```csharp
using Finbuckle.MultiTenant;

var builder = WebApplication.CreateBuilder(args);

 // ...add app services

// add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie()
       .AddOpenIdConnect();

// add Finbuckle.MultiTenant services
builder.Services.AddMultiTenant<TenantInfo>()
       .WithRouteStrategy()
       .WithConfigurationStore()
       .WithPerTenantAuthentication();

var app = builder.Build();

// add the Finbuckle.MultiTenant middleware
app.UseMultiTenant();

// ...add other middleware

app.Run();
```

The code above paired with the `appsettings.json` tenant configuration below
produces the behavior described. Any tenant store configured similarly will also
work.

```json
{
  "Finbuckle:MultiTenant:Stores:ConfigurationStore": {
    "Defaults": {
      "ConnectionString": "",
      "CookieLoginPath": "/__tenant__/home/login",
      "CookieLogoutPath": "/__tenant__/home/logout"
    },
    "Tenants": [
      {
        "Id": "93f330717e5d4f039cd05da312d559cc",
        "Identifier": "megacorp",
        "Name": "MegaCorp",
        "ChallengeScheme": "Cookies"
      },
      {
        "Id": "505c5c97f4e2442394610c673ac91f61",
        "Identifier": "acme",
        "Name": "ACME",
        "ChallengeScheme": "OpenIdConnect",
        "OpenIdConnectAuthority": "https://finbuckle-acme.us.auth0.com",
        "OpenIdConnectClientId": "2lGONpJBwIqWuN2QDAmBbYGt0k0khwQB",
        "OpenIdConnectClientSecret": "HWxQfz6U8GvPCSsvfH5U3uv6CzAeQSt8qHrc19_qEvUQhdsaJX9Dp-t9W-5SAj0m"
      },
      {
        "Id": "4ee609d6da0342e682012232566cff0e",
        "Identifier": "initech",
        "Name": "Initech",
        "ChallengeScheme": "OpenIdConnect",
        "OpenIdConnectAuthority": "https://finbuckle-initech.us.auth0.com",
        "OpenIdConnectClientId": "nmPF6VABNmzTISvtYLPenf08ARveQifZ",
        "OpenIdConnectClientSecret": "WINWtT2WAhWYUOgGHsAPIUV-dAHs1X4qcU6Pv98HBrorlOB5OMKetnsR0Ov0LuVm"
      }
    ]
  }
}
```

## Other Authentication Options

Internally `WithPerTenantAuthentication()` makes use of
[per-tenant options](Options). For authentication options not covered by
`WithPerTenantAuthentication()`, per-tenant option can provide similar behavior.

For example, if you want to configure JWT tokens so that each tenant has a
different recognized authority for token validation we can add a field to the
`ITenantInfo` implementation and configure the option per-tenant. Any options configured will overwrite earlier
configurations:

```csharp 
builder.Services.AddMultiTenant<TenantInfo>()
        .WithConfigurationStore()
        .WithRouteStrategy()
        .WithPerTenantAuthentication()

// WithPerTenantAuthentication, as shown above, is needed for this to work as intended.
// Note the default JwtBearer authentication scheme is used for the options name per AspNetCore defauls,
// but you can use a custom authentication scheme name to scope the options or use ConfigureAllPerTenant
// to impact all authentication schemes.
builder.Services.ConfigurePerTenant<JwtBearerOptions, TenantInfo>(JwtBearerDefaults.AuthenticationScheme, (options, 
tenantInfo) =>
    {
        // assume tenants are configured with an authority string to use here.
        options.Authority = tenantInfo.JwtAuthority;
    }
```

The same approach can be used for cookie, OpenID Connect, or any other
authentication options type.

Another common use case is the need to have separate cookies per tenant in
addition to the functionality provided by `WithPerTenantAuthentication` which by
default only uses a single cookie for all tenants. By using per-tenant options
we can give each tenant's cookie a different name. This effectively maintains
existing tenant sign-ins when switching between requests on the same browser or
agent because new sign-ins are not replacing the existing cookie:

```csharp
builder.Services.AddMultiTenant<TenantInfo>()
        .WithConfigurationStore()
        .WithRouteStrategy()
        .WithPerTenantAuthentication()

// WithPerTenantAuthentication, as shown above, is needed for this to work as intended.
// Note the default cookie authentication scheme is used for the options name per AspNetCore defauls,
// but you can use a custom authentication scheme name to scope the options or use ConfigureAllPerTenant
// to impact all authentication schemes.
builder.Services.ConfigurePerTenant<CookieAuthenticationOptions, TenantInfo>(CookieAuthenticationDefaults.AuthenticationScheme, (options, tenantInfo) =>
  {
    options.Cookie.Name = "SignInCookie-" + tenantInfo.Id;
  });
```
