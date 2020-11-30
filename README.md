# app-provisonning-tool
Tool to create Microsoft identity platform applications in a tenant (AAD or B2C) and update the configuration code of the applications

## Installing/Uninstalling the tool

1. Build the repository
2. Run the following in a developer command prompt in the root of the `src\DotnetTool` folder:
   
   ```Shell
   dotnet tool install --global --add-source ./nupkg msIdentityApp
   ```

If later you want to uninstall the tool, just run:
```Shell
dotnet tool uninstall --global msidentityapp
```

## Scenarios

<table>
   <tr> <td>Family of scenarios (initial state of the code)</td> <td>Scenario</td> </tr>
   <tr> <td rowspan="3">ASP.NET Core web apps / apis where Authentication was enabled </td> 
      <td>Configure the code with a new app registration</td>
   </tr>
   <tr> <td>Configure the code with an existing app registration</td></tr>
   <tr> <td>Update the app registration based on the code (or troubleshoot)</td></tr>
   <tr> <td>ASP.NET Core web apps / apis where Authentication was not enabled </td><td> Update the code to add authentication</td>
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
