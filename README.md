# TAG API

Clean architecture .NET 10 Web API with JWT authentication, rotating refresh tokens and a
role / permission (policy) system that an administrator manages at runtime.

```
Domain           entities only, no dependencies
Application      services, DTOs, repository contract, permission catalog
Infrastructure   EF Core context, configurations, migrations, seed data, repository
API              controllers, authorization plumbing, middleware, composition root
```

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

## Shipping it

Replace `Jwt:Key` and the seeded super admin password before the API leaves a development
machine. Both belong in environment variables or user secrets, not in `appsettings.json`.
