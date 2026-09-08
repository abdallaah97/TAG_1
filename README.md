# TAG API

Clean architecture .NET 10 Web API with JWT authentication, rotating refresh tokens and a
role / permission (policy) system that an administrator manages at runtime.

```
Domain           entities only, no dependencies
Application      services, DTOs, unit of work and repository contracts, permission catalog
Infrastructure   EF Core context, configurations, migrations, seed data, unit of work
API              controllers, authorization plumbing, middleware, composition root
```

## Persistence

A service never touches the `DbContext` and never injects a repository directly, it injects
`IUnitOfWork` (`Application/Repositories/IUnitOfWork.cs`). The unit of work is scoped, exactly
like the context it wraps, and hands out one cached `IGenericRepository<T>` per entity:

```csharp
var user = await _unitOfWork.Users.GetByIdAsync(id);
user.IsActive = false;
_unitOfWork.Users.Update(user);

await _userSecurityService.RevokeUserTokensAsync(user.Id, RevokeReasons.UserDeactivated);

// Rows from two different tables, one commit.
await _unitOfWork.SaveChangesAsync();
```

The repositories only read and stage; `SaveChanges` lives on the unit of work alone. That is what
makes the guard rails hold: disabling an account and killing its sessions, or swapping the roles
of a user and dropping the tokens that still carry the old ones, either happen together or not at
all. A service method stages everything it needs and commits once at the end.

Entities without a dedicated property on `IUnitOfWork` are reachable through
`_unitOfWork.Repository<T>()`.

## Running it

```bash
dotnet run --project API/API.csproj
```

The connection string lives in `API/appsettings.json` (`ConnectionStrings:Default`).
On startup the application applies the pending migrations and makes sure a super admin exists,
so nothing has to be run by hand. To do it manually anyway:

```bash
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project API/API.csproj
```

Swagger: `http://localhost:5119/swagger`

Seeded account (`Seed:SuperAdmin` in `appsettings.json`):

| Email | Password |
| --- | --- |
| admin@tag.com | Admin@123 |

## Authentication

`POST /api/Auth/Login` returns a short lived access token (15 minutes) plus a refresh token
(7 days), together with the roles and permissions of the account.

The access token carries the identity, one `role` claim per role and one `permission` claim per
permission, so authorizing a request costs no database round trip.

**Refresh tokens are rotated.** Every call to `POST /api/Auth/RefreshToken` revokes the token it
was given and hands back a new pair, rebuilt from the roles and permissions the user holds at
that moment. Only the SHA-256 hash of a refresh token is stored, so a leaked database can not be
replayed. If a token that was already rotated away is presented again, the replay is treated as a
theft: every session of that user is revoked at once.

Sessions are also cut whenever what a user is allowed to do changes: the password is changed, the
account is disabled, the roles of the user change, or the permissions of one of their roles
change. The current access token stays valid until it expires (at most 15 minutes), and the next
refresh forces a fresh sign in.

| Endpoint | Purpose |
| --- | --- |
| `POST /api/Auth/Login` | sign in |
| `POST /api/Auth/RefreshToken` | rotate the refresh token and get a new access token |
| `POST /api/Auth/Logout` | revoke one refresh token |
| `POST /api/Auth/LogoutAll` | revoke every session of the caller |
| `GET /api/Auth/Me` | the caller with their roles and permissions |
| `POST /api/Auth/ChangePassword` | change own password |
| `PUT /api/Auth/UpdateProfile` | change own name, e-mail and phone |

## Roles and permissions

A permission is one atomic right, e.g. `Users.Create`. The catalog is code
(`Application/Common/Authorization/Permissions.cs`) and the `Permissions` table is only a mirror
of it, seeded through a migration. Roles are data: an administrator creates them and picks which
permissions they carry. A user can hold several roles and their effective permission set is the
union of them.

Endpoints are guarded declaratively:

```csharp
[HasPermission(Permissions.Users.Create)]
[HttpPost("CreateUser")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserDto input) { ... }
```

`HasPermissionAttribute` (`Application/Common/Authorization/HasPermissionAttribute.cs`) is itself
the authorization filter: it checks the caller is authenticated, then allows the request if the
user is `SuperAdmin` or the `permission` claim it needs is present in the token. No policy
registration or separate handler is involved.

### Adding a permission

1. Add the constant and an entry in `Permissions.All` with the next free id.
2. `dotnet ef migrations add AddXyzPermission --project Infrastructure/Infrastructure.csproj --startup-project API/API.csproj`
3. Put `[HasPermission(...)]` on the endpoint.

### Role endpoints

| Endpoint | Required permission |
| --- | --- |
| `GET /api/Roles/GetAllRoles` | `Roles.View` |
| `GET /api/Roles/GetRoleById` | `Roles.View` |
| `GET /api/Roles/GetAllPermissions` | `Roles.View` |
| `GET /api/Roles/GetRolePermissions` | `Roles.View` |
| `GET /api/Roles/GetRoleUsers` | `Roles.View` |
| `POST /api/Roles/CreateRole` | `Roles.Create` |
| `PUT /api/Roles/UpdateRole` | `Roles.Update` |
| `DELETE /api/Roles/DeleteRole` | `Roles.Delete` |
| `PUT /api/Roles/UpdateRolePermissions` | `Roles.ManagePermissions` |
| `POST /api/Roles/AddPermissionToRole` | `Roles.ManagePermissions` |
| `DELETE /api/Roles/RemovePermissionFromRole` | `Roles.ManagePermissions` |

### User endpoints

| Endpoint | Required permission |
| --- | --- |
| `GET /api/Users/GetAllUsers` | `Users.View` |
| `GET /api/Users/GetUserById` | `Users.View` |
| `GET /api/Users/ExportUsers` | `Users.View` |
| `POST /api/Users/CreateUser` | `Users.Create` |
| `PUT /api/Users/UpdateUser` | `Users.Update` |
| `PUT /api/Users/SetUserActiveState` | `Users.Update` |
| `DELETE /api/Users/DeleteUser` | `Users.Delete` |
| `POST /api/Users/ChangePassword` | `Users.ChangePassword` |
| `PUT /api/Users/AssignRoles` | `Users.AssignRoles` |
| `POST /api/Users/AddRoleToUser` | `Users.AssignRoles` |
| `DELETE /api/Users/RemoveRoleFromUser` | `Users.AssignRoles` |

`AssignRoles` and `UpdateRolePermissions` replace the whole set; the `Add`/`Remove` endpoints
change a single entry.

### Guard rails

- A system role (`SuperAdmin`, `Admin`, `User`) can not be renamed or deleted.
- The permissions of `SuperAdmin` can not be changed, it always holds everything.
- A role that is still assigned to users can not be deleted.
- The system can never be left without an active super admin.
- Nobody can delete or disable their own account.

## Rate limiting

`API/Middleware/RateLimitMiddleware.cs` counts the requests of one caller inside a fixed window
and answers `429 Too Many Requests` once the limit is used up. It runs after `UseAuthentication`,
so a signed in caller is counted per account (`id` claim) and only an anonymous one per IP.

The counters live in `IMemoryCache`, one entry per caller with an absolute expiration: when the
entry expires the window is over and the caller starts from zero again.

```json
"RateLimit": {
  "PermitLimit": 100,
  "WindowSeconds": 60
}
```

The counters are per process, so two instances behind a load balancer allow twice the limit; a
shared store (`IDistributedCache`, Redis) is what replaces `IMemoryCache` at that point.

## Design patterns

Two small features are built as **Strategy + Factory**. Both have the same shape: one
interface, a couple of classes that implement it, and a factory that is the only thing that
picks which one is used.

| | Strategy | Factory |
| --- | --- | --- |
| Answers | *how* the work is done | *which* one is used |
| Password rules | `IPasswordPolicy` | `IPasswordPolicyFactory` |
| Export formats | `IUserExportStrategy` | `IUserExportStrategyFactory` |

### Password policies

`Application/Common/Security/` holds two policies:

| `Security:PasswordPolicy` | Rule |
| --- | --- |
| `Basic` | at least 6 characters |
| `Strong` | at least 8 characters, one upper case letter, one digit and one symbol |

The factory reads the setting on every call, so editing `appsettings.json` while the API is
running changes the rules on the next request. Every call site looks the same and never names
a concrete policy:

```csharp
var policy = _passwordPolicyFactory.Create();

if (!policy.IsValid(input.Password))
{
    throw new BadRequestException($"Weak password. {policy.Description}");
}
```

### Export formats

`GET /api/Users/ExportUsers?format=csv` (or `json`) reuses the normal user listing and hands
the rows to a strategy. `UserService.ExportUsers` never learns which format came out; an
unknown format is a 400 from the factory.

Adding Excel would be one new class plus one line in `UserExportStrategyFactory`.

### Already here before the demo

| In the code | Pattern |
| --- | --- |
| `IUnitOfWork.Repository<T>()` | Factory |
| `IPasswordHasher<User>` injected into the services | Strategy |
| `IGenericRepository<T>` | Repository |
| `IUnitOfWork` | Unit of Work |
| `ExceptionHandlingMiddleware` and the rest of the pipeline | Chain of Responsibility |

## Shipping it

Replace `Jwt:Key` and the seeded super admin password before the API leaves a development
machine. Both belong in environment variables or user secrets, not in `appsettings.json`.
