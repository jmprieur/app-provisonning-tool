# ms-identity-app 
Command line tool that creates Microsoft identity platform applications in a tenant (AAD or B2C) and updates the configuration code of you ASP.NET Core applications.

## Installing/Uninstalling the tool

1. Build the repository and create the NuGet package (from the `src\DotnetTool` folder):
 
   ```Shell
   dotnet pack
   ```
   
2. Run the following in a developer command prompt in the `src\DotnetTool` folder:
   
   ```Shell
   dotnet tool install --global --add-source ./nupkg msIdentityApp
   ```

If later you want to uninstall the tool, just run:
```Shell
dotnet tool uninstall --global msidentityapp
```

## Pre-requisites to using the tool

Have an AAD or B2C tenant (or both). 
- If you want to add an AAD registration, you are usually already signed-in in Visual Studio in a tenant. If needed you can create your own tenant by following this quickstart [Setup a tenant](https://docs.microsoft.com/azure/active-directory/develop/quickstart-create-new-tenant). But be sure to sign-out and sign-in from Visual Studio or Azure CLI so that this tenant is known in the shared token cache.

- If you want to add a AAD B2C registration you'll need a B2C tenant, and explicity pass it to the `--tenant-id` option of the tool. To create a B2C tenant, see [Create a B2C tenant](https://docs.microsoft.com/azure/active-directory-b2c/tutorial-create-tenant)


## Scenarios

<table>
   <tr> <td><b>Family of scenarios (initial state of the code)</b></td> <td><b>Scenario</b></td> </tr>
   <tr> <td rowspan="3">ASP.NET Core web apps / apis where Authentication was enabled (`--auth``)</td> 
      <td><b>Supported today</b>: Create a new app registration adn configure the code. This works for all projects (mvc, webapp, webapi, blazorserver, blazorwasm (hosted or not), and both for AAD and B2C</td>
   </tr>
   <tr> <td><b>Supported today</b>: Configure the code with an existing app registration. You need to pass-in a clientId, and the app will be configured from it.</td></tr>
   <tr> <td><b>Partially supported today</b>: Update the app registration based on the code (or troubleshoot). For instance when you have changed the app launch URLs</td></tr>
   <tr> <td>ASP.NET Core web apps / apis where Authentication was not enabled </td><td><b>Future work</b>: Update the code to add authentication</td>
   </tr>
 </table>

### ASP.NET Core web apps / apis where Authentication was enabled
 
#### Configure the code with a new app registration

##### Usage
Go to a folder containing an ASP.NET Core 3.1 or 5 application where authentication was enabled, but not configured

```Shell
ms-identity-app [--tenant-id yourTenantId] [--username username@domain.com]
```

example

```Shell
cd folder-of-my-app
dotnet new webapp --auth SingleOrg
ms-identity-app --tenant-id testprovisionningtool.onmicrosoft.com
```

```Shell
cd folder-of-my-b2c-app
dotnet new webapp --auth IndividualB2C
ms-identity-app --tenant-id fabrikamb2c.onmicrosoft.com
```

Will: 
- detect the kind of application (web app, web api, blazor server, blazor web assembly, hosted or not)
- detect the IDP (AAD or B2C*)
- create a new app registration in the tenant, using your developer credentials if possible (and prompting you otherwise)
- update the configuration files (and program.cs for Blazor apps)

Currently works for:

- [x] AAD apps
- [x] B2C apps (except Blazorwasm hosted apps for the moment)

In
- [x] web apps
- [x] web apis
- [x] blazor web assembly
- [x] blazor web assembly hosted **except B2C**

Where these apps:
- [x] call Graph or not
- [x] call a downstream API or not


##### Parameters
Parameter | Description
--------- | ------------
`--tenant-id <tenantid>` | If specified, the tool will create the application in the specified tenant. Otherwise it will create the app in your home tenant ID
`--username <someone@domain.com>` | Needed when you are signed-in in Visual Studio, or Azure CLI with several identities. In that case username is used to disambiguate which identity to use.
`--folder '<pathToFolder>'` | When specified, will analyze the application code in the specified folder. Otherwise analyzes the code in the current directory

##### Supported frameworks

The tool supports ASP.NET Core applications created with .NET 5.0 and netcoreapp3.1. In the case of netcoreapp3.1, for blazorwasm applicaitons, the redirect URI created for the app is a "Web" redirect URI (as Blazor web assembly leverages MSAL.js 1.x in netcoreapp3.1), whereas in net5.0 it's a "SPA" redirect URI (as Blazor web assembly leverages MSAL.js 2.x in net5.0) 

```Shell
dotnet new blazorwasm --auth SingleOrg --framework netcoreapp3.1
ms-identity-app
dotnet run -f netstandard2.1
```

#### Configure the code with an existing app registration

This is the same, but this time you'll specify the clientId of an existing application registration.
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


#### Update the app registration based on the code

Future scenario being developped

###  ASP.NET Core web apps / apis where Authentication was not enabled

#### Update the code to add authentication

Future scenario
