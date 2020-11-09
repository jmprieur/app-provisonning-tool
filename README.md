# app-provisonning-tool
Tool to provision Microsoft identity platform applications and sync it with code configuration

## Scenarios

### ASP.NET Core web apps / apis where Authentication was enabled

#### Configure a green field application (new app registration)

Go to a folder containing an ASP.NET Core 3.1 or 5 application where authentication was enabled, but not configured

```Shell
app-provisionning-tool [--tenant-id yourTenantId] [--username username@domain.com]
```

example

```Shell
cd folder-of-my-app
app-provisionning-tool --client-id 18b26764-9897-4c83-9bee-a5da835d5f29 --tenant-id 7f58f645-c190-4ce5-9de4-e2b7acd2a6ab
```

Will: 
- detect the kind of application (web app, web api, blazor app)
- detect the IDP (AAD or B2C*)
- create a new app registration in the tenant, using your developer credentials if possible (and prompting you otherwise)
- update the configuration files (and program.cs for Blazor apps)

Works for:

- [ ] AAD
- [ ] B2C

In
- [ ] web apps
- [ ] web apis
- [ ] blazor web assembly
- [ ] blazor web assembly hosted

#### Configure a green field application (existing app registration)

Go to a folder containing an ASP.NET Core 3.1 or 5 application where authentication was enabled, but not configured

```Shell
app-provisionning-tool [--username username@domain.com] [--tenant-id yourTenantId] --client-id <yourClientId>
```

example

```Shell
cd folder-of-my-app
app-provisionning-tool --client-id 18b26764-9897-4c83-9bee-a5da835d5f29 --tenant-id 7f58f645-c190-4ce5-9de4-e2b7acd2a6ab
```

uses your developer credentials to find application "18b26764-9897-4c83-9bee-a5da835d5f29" in tenant "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab" and update the settings files from the app registration (apart from the client secret which cannot be retrieved from Azure AD)