﻿using Azure.Core;
using DotnetTool.AuthenticationParameters;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetTool.MicrosoftIdentityPlatformApplication
{
    public class MicrosoftIdentityPlatformApplicationManager
    {
        const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";
        const string ScopeType = "Scope";

        GraphServiceClient? _graphServiceClient;

        internal async Task<ApplicationParameters> CreateNewApp(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            // Get the tenant
            var tenant = (await graphServiceClient.Organization
                .Request()
                .GetAsync()).FirstOrDefault();

            // Create the app.
            Application application = new Application()
            {
                DisplayName = applicationParameters.DisplayName,
                SignInAudience = AppParameterAudienceToMicrosoftIdentityPlatformAppAudience(applicationParameters.SignInAudience!),
                Description = applicationParameters.Description
            };


            if (applicationParameters.IsWebApi)
            {
                application.Api = new ApiApplication()
                {
                    RequestedAccessTokenVersion = 2,
                };
            }

            if (applicationParameters.IsWebApp)
            {
                AddWebAppPlatform(applicationParameters, application);
            }
            else if (applicationParameters.IsBlazor)
            {
                // In .NET Core 3.1, Blazor uses MSAL.js 1.x (web redirect URIs)
                // whereas in .NET 5.0, Blazor uses MSAL.js 2.x (SPA redirect URIs)
                if (applicationParameters.TargetFramework == "net5.0")
                {
                    AddSpaPlatform(applicationParameters, application);
                }
                else
                {
                    AddWebAppPlatform(applicationParameters, application, true);
                }
            }

            var scopesPerResource = await AddApiPermissions(
                applicationParameters,
                graphServiceClient,
                application);
            Application createdApplication = await graphServiceClient.Applications
                .Request()
                .AddAsync(application);

            // Creates a service principal (needed for B2C)
            ServicePrincipal servicePrincipal = new ServicePrincipal
            {
                AppId = createdApplication.AppId,
            };

            // B2C does not allow user consent, and therefore we need to explicity create
            // a service principal and permission grants. It's also useful for Blazorwasm hosted
            // applications. We choose to create it always
            var createdServicePrincipal = await graphServiceClient.ServicePrincipals
                .Request()
                .AddAsync(servicePrincipal);

            // B2C does not allow user consent, and therefore we need to explicity grant permissions
            if (applicationParameters.IsB2C)
            {
                await AddAdminConsentToApiPermissions(
                    graphServiceClient,
                    createdServicePrincipal,
                    scopesPerResource);
            }

            // For web API, we need to know the appId of the created app to compute the Identifier URI, 
            // and therefore we need to do it after the app is created (updating the app)
            if (applicationParameters.IsWebApi
                && createdApplication.Api != null
                && (createdApplication.IdentifierUris == null || !createdApplication.IdentifierUris.Any()))
            {
                await ExposeScopes(graphServiceClient, createdApplication);

                // Blazorwasm hosted: add permission to serer web api from client SPA
                if (applicationParameters.IsBlazor)
                {
                    await AddApiPermissionFromBlazorwasmHostedSpaToServerApi(graphServiceClient,
                        createdApplication,
                        createdServicePrincipal,
                        applicationParameters.IsB2C);
                }
            }

            // Re-reading the app to be sure to have everything.
            createdApplication = (await graphServiceClient.Applications
                .Request()
                .Filter($"appId eq '{createdApplication.AppId}'")
                .GetAsync()).First();

            var effectiveApplicationParameters = GetEffectiveApplicationParameters(tenant, createdApplication, applicationParameters);

            // Add password credentials
            if (applicationParameters.CallsMicrosoftGraph || applicationParameters.CallsDownstreamApi)
            {
                await AddPasswordCredentials(
                    graphServiceClient,
                    createdApplication,
                    effectiveApplicationParameters);
            }

            return effectiveApplicationParameters;
        }

        internal async Task UpdateApplication(TokenCredential tokenCredential, ApplicationParameters reconcialedApplicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            var existingApplication = (await graphServiceClient.Applications
               .Request()
               .Filter($"appId eq '{reconcialedApplicationParameters.ClientId}'")
               .GetAsync()).First();

            // Updates the redirect URIs
            var updatedApp = new Application
            {
                Web = existingApplication.Web
            };
            updatedApp.Web.RedirectUris = reconcialedApplicationParameters.WebRedirectUris;

            // TODO: update other fields. 
            // See https://github.com/jmprieur/app-provisonning-tool/issues/10
            await graphServiceClient.Applications[existingApplication.Id]
                .Request()
                .UpdateAsync(updatedApp);

            if (existingApplication.RequiredResourceAccess != null
                && existingApplication.RequiredResourceAccess.Any()
                && (existingApplication.PasswordCredentials == null
                || !existingApplication.PasswordCredentials.Any()
                || !existingApplication.PasswordCredentials.Any(password => !string.IsNullOrEmpty(password.SecretText))))
            {
                await AddPasswordCredentials(graphServiceClient, existingApplication, reconcialedApplicationParameters);
            }
        }

        private async Task AddApiPermissionFromBlazorwasmHostedSpaToServerApi(
            GraphServiceClient graphServiceClient,
            Application createdApplication,
            ServicePrincipal createdServicePrincipal,
            bool isB2C)
        {
            var requiredResourceAccess = new List<RequiredResourceAccess>();
            var resourcesAccessAndScopes = new List<ResourceAndScope>
                {
                    new ResourceAndScope($"api://{createdApplication.AppId}", "access_as_user")
                    {
                         ResourceServicePrincipalId = createdServicePrincipal.Id
                    }
                };

            await AddPermission(
                graphServiceClient,
                requiredResourceAccess,
                resourcesAccessAndScopes.GroupBy(r => r.Resource).First());

            Application applicationToUpdate = new Application
            {
                RequiredResourceAccess = requiredResourceAccess
            };

            await graphServiceClient.Applications[createdApplication.Id]
                .Request()
                .UpdateAsync(applicationToUpdate);

            if (isB2C)
            {
                var oAuth2PermissionGrant = new OAuth2PermissionGrant
                {
                    ClientId = createdServicePrincipal.Id,
                    ConsentType = "AllPrincipals",
                    PrincipalId = null,
                    ResourceId = createdServicePrincipal.Id,
                    Scope = "access_as_user"
                };

                await graphServiceClient.Oauth2PermissionGrants
                    .Request()
                    .AddAsync(oAuth2PermissionGrant);
            }
        }

        /// <summary>
        /// Add a password credential to the app
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="createdApplication"></param>
        /// <param name="effectiveApplicationParameters"></param>
        /// <returns></returns>
        private static async Task AddPasswordCredentials(GraphServiceClient graphServiceClient, Application createdApplication, ApplicationParameters effectiveApplicationParameters)
        {
            var passwordCredential = new PasswordCredential
            {
                DisplayName = "Password created by the provisionning tool"
            };

            PasswordCredential returnedPasswordCredential = await graphServiceClient.Applications[$"{createdApplication.Id}"]
                .AddPassword(passwordCredential)
                .Request()
                .PostAsync();
            effectiveApplicationParameters.PasswordCredentials.Add(returnedPasswordCredential.SecretText);
        }

        /// <summary>
        /// Expose scopes for the web API
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="createdApplication"></param>
        /// <returns></returns>
        private static async Task ExposeScopes(GraphServiceClient graphServiceClient, Application createdApplication)
        {
            var updatedApp = new Application
            {
                IdentifierUris = new[] { $"api://{createdApplication.AppId}" },
            };
            var scopes = createdApplication.Api.Oauth2PermissionScopes?.ToList() ?? new List<PermissionScope>();
            var newScope = new PermissionScope
            {
                Id = Guid.NewGuid(),
                AdminConsentDescription = "Allows the app to access the web API on behalf of the signed-in user",
                AdminConsentDisplayName = "Access the API on behalf of a user",
                Type = "User",
                IsEnabled = true,
                UserConsentDescription = "Allows this app to access the web API on your behalf",
                UserConsentDisplayName = "Access the API on your behalf",
                Value = "access_as_user",
            };
            scopes.Add(newScope);
            updatedApp.Api = new ApiApplication { Oauth2PermissionScopes = scopes };

            await graphServiceClient.Applications[createdApplication.Id]
                .Request()
                .UpdateAsync(updatedApp);
        }

        /// <summary>
        /// Admin consent to API permissions
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="scopesPerResource"></param>
        /// <returns></returns>
        private static async Task AddAdminConsentToApiPermissions(
            GraphServiceClient graphServiceClient,
            ServicePrincipal servicePrincipal,
            IEnumerable<IGrouping<string, ResourceAndScope>> scopesPerResource)
        {
            // Consent to the scopes
            if (scopesPerResource != null)
            {
                foreach (var g in scopesPerResource)
                {
                    IEnumerable<ResourceAndScope> resourceAndScopes = g;

                    var oAuth2PermissionGrant = new OAuth2PermissionGrant
                    {
                        ClientId = servicePrincipal.Id,
                        ConsentType = "AllPrincipals",
                        PrincipalId = null,
                        ResourceId = resourceAndScopes.FirstOrDefault().ResourceServicePrincipalId,
                        Scope = string.Join(" ", resourceAndScopes.Select(r => r.Scope))
                    };

                    // TODO: See https://github.com/jmprieur/app-provisonning-tool/issues/9. 
                    // We need to process the case where the developer is not a tenant admin
                    await graphServiceClient.Oauth2PermissionGrants
                        .Request()
                        .AddAsync(oAuth2PermissionGrant);
                }
            }
        }

        /// <summary>
        /// Add API permissions
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="graphServiceClient"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        private async Task<IEnumerable<IGrouping<string, ResourceAndScope>>> AddApiPermissions(ApplicationParameters applicationParameters, GraphServiceClient graphServiceClient, Application application)
        {
            // Case where the app calls a downstream API
            List<RequiredResourceAccess> apiRequests = new List<RequiredResourceAccess>();
            string calledApiScopes = applicationParameters.CalledApiScopes;
            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource = null;
            if (!string.IsNullOrEmpty(calledApiScopes))
            {
                string[] scopes = calledApiScopes.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
                scopesPerResource = scopes.Select(s => (!s.Contains('/'))
                // Microsoft graph shortcut scopes (for instance "User.Read")
                ? new ResourceAndScope("https://graph.microsoft.com", s)
                // Proper AppIdUri/scope
                : new ResourceAndScope(s.Substring(0, s.LastIndexOf('/')), s.Substring(s.LastIndexOf('/') + 1))
                ).GroupBy(r => r.Resource)
                .ToArray(); // We want to modify these elements to cache the service principal ID

                foreach (var g in scopesPerResource)
                {
                    await AddPermission(graphServiceClient, apiRequests, g);
                }
            }

            if (apiRequests.Any())
            {
                application.RequiredResourceAccess = apiRequests;
            }

            return scopesPerResource;
        }

        /// <summary>
        /// Adds a SPA redirect URI
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="application"></param>
        private static void AddSpaPlatform(ApplicationParameters applicationParameters, Application application)
        {
            // The Graph SDK does not expose the .Spa platform yet.
            application.AdditionalData = new Dictionary<string, object>();
            application.AdditionalData.Add("spa",
                new Spa
                {
                    redirectUris = applicationParameters.WebRedirectUris
                });
        }

        /// <summary>
        /// Adds the Web redirect URIs (and required scopes in the case of B2C web apis)
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="application"></param>
        /// <param name="withImplicitFlow">Should it add the implicit flow access token (for Blazor in netcore3.1)</param>
        private static void AddWebAppPlatform(ApplicationParameters applicationParameters, Application application, bool withImplicitFlow=false)
        {
            application.Web = new WebApplication();

            // IdToken
            if ( (!applicationParameters.CallsDownstreamApi && !applicationParameters.CallsMicrosoftGraph)
                || withImplicitFlow)
            {
                application.Web.ImplicitGrantSettings = new ImplicitGrantSettings();
                application.Web.ImplicitGrantSettings.EnableIdTokenIssuance = true;
                if (applicationParameters.IsB2C || withImplicitFlow)
                {
                    application.Web.ImplicitGrantSettings.EnableAccessTokenIssuance = true;
                }
            }

            // Redirect URIs
            application.Web.RedirectUris = applicationParameters.WebRedirectUris;

            // Logout URI
            application.Web.LogoutUrl = applicationParameters.LogoutUrl;

            // Explicit usage of MicrosoftGraph openid and offline_access, in the case
            // of Azure AD B2C.
            if (applicationParameters.IsB2C && applicationParameters.IsWebApp || applicationParameters.IsBlazor)
            {
                if (applicationParameters.CalledApiScopes == null)
                {
                    applicationParameters.CalledApiScopes = string.Empty;
                }
                if (!applicationParameters.CalledApiScopes.Contains("openid"))
                {
                    applicationParameters.CalledApiScopes += " openid";
                }
                if (!applicationParameters.CalledApiScopes.Contains("offline_access"))
                {
                    applicationParameters.CalledApiScopes += " offline_access";
                }
                applicationParameters.CalledApiScopes = applicationParameters.CalledApiScopes.Trim();
            }
        }


        /// <summary>
        /// Adds API permissions
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="apiRequests"></param>
        /// <param name="g"></param>
        /// <returns></returns>
        private async Task AddPermission(
            GraphServiceClient graphServiceClient,
            List<RequiredResourceAccess> apiRequests,
            IGrouping<string, ResourceAndScope> g)
        {

            var spsWithScopes = await graphServiceClient.ServicePrincipals
                .Request()
                .Filter($"servicePrincipalNames/any(t: t eq '{g.Key}')")
                .GetAsync();

            // Special case for B2C where the service principal does not contain the graph URL :(
            if (!spsWithScopes.Any() && g.Key == "https://graph.microsoft.com")
            {
                spsWithScopes = await graphServiceClient.ServicePrincipals
                                .Request()
                                .Filter($"AppId eq '{MicrosoftGraphAppId}'")
                                .GetAsync();
            }
            var spWithScopes = spsWithScopes.FirstOrDefault();

            // Keep the service principal ID for later
            foreach (ResourceAndScope r in g)
            {
                r.ResourceServicePrincipalId = spWithScopes.Id;
            }

            IEnumerable<string> scopes = g.Select(r => r.Scope.ToLower());
            var permissionScopes = spWithScopes.Oauth2PermissionScopes
                .Where(s => scopes.Contains(s.Value.ToLower()));

            RequiredResourceAccess requiredResourceAccess = new RequiredResourceAccess
            {
                ResourceAppId = spWithScopes.AppId,
                ResourceAccess = new List<ResourceAccess>(permissionScopes.Select(p =>
                 new ResourceAccess
                 {
                     Id = p.Id,
                     Type = ScopeType
                 }))
            };
            apiRequests.Add(requiredResourceAccess);
        }

        /// <summary>
        /// Computes the audience
        /// </summary>
        /// <param name="audience"></param>
        /// <returns></returns>
        private string MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(string audience)
        {
            switch (audience)
            {
                case "AzureADMyOrg":
                    return "SingleOrg";
                case "AzureADMultipleOrgs":
                    return "MultiOrg";
                case "AzureADandPersonalMicrosoftAccount":
                    return "MultiOrgAndPersonal";
                case "PersonalMicrosoftAccount":
                    return "Personal";
                default:
                    return "SingleOrg";
            }
        }

        private string AppParameterAudienceToMicrosoftIdentityPlatformAppAudience(string audience)
        {
            switch (audience)
            {
                case "SingleOrg":
                    return "AzureADMyOrg";
                case "MultiOrg":
                    return "AzureADMultipleOrgs";
                case "Personal":
                    return "PersonalMicrosoftAccount";
                case "MultiOrgAndPersonal":
                default:
                    return "AzureADandPersonalMicrosoftAccount";
            }
        }

        internal async Task Unregister(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            var apps = await graphServiceClient.Applications
                .Request()
                .Filter($"appId eq '{applicationParameters.ClientId}'")
                .GetAsync();

            var readApplication = apps.FirstOrDefault();
            if (readApplication != null)
            {
                await graphServiceClient.Applications[$"{readApplication.Id}"]
                    .Request()
                    .DeleteAsync();
            }
        }

        private GraphServiceClient GetGraphServiceClient(TokenCredential tokenCredential)
        {
            if (_graphServiceClient == null)
            {
                _graphServiceClient = new GraphServiceClient(new TokenCredentialAuthenticationProvider(tokenCredential));
            }
            return _graphServiceClient;
        }

        public async Task<ApplicationParameters?> ReadApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            var tenant = (await graphServiceClient.Organization
                .Request()
                .GetAsync()).FirstOrDefault();

            var apps = await graphServiceClient.Applications
                .Request()
                .Filter($"appId eq '{applicationParameters.ClientId}'")
                .GetAsync();

            var readApplication = apps.FirstOrDefault();

            if (readApplication == null)
            {
                return null;
            }

            ApplicationParameters effectiveApplicationParameters = GetEffectiveApplicationParameters(tenant, readApplication, applicationParameters);

            return effectiveApplicationParameters;

        }

        private ApplicationParameters GetEffectiveApplicationParameters(
            Organization tenant,
            Application application,
            ApplicationParameters originalApplicationParameters)
        {
            bool isB2C = (tenant.TenantType == "AAD B2C");
            var effectiveApplicationParameters = new ApplicationParameters
            {
                DisplayName = application.DisplayName,
                ClientId = application.AppId,
                IsAAD = !isB2C,
                IsB2C = isB2C,
                HasAuthentication = true,
                IsWebApi = application.Api != null
                        && (application.Api.Oauth2PermissionScopes != null && application.Api.Oauth2PermissionScopes.Any())
                        || (application.AppRoles != null && application.AppRoles.Any()),
                IsWebApp = application.Web != null,
                TenantId = tenant.Id,
                Domain = tenant.VerifiedDomains.FirstOrDefault(v => v.IsDefault.HasValue && v.IsDefault.Value)?.Name,
                CallsMicrosoftGraph = application.RequiredResourceAccess.Any(r => r.ResourceAppId == MicrosoftGraphAppId) && !isB2C,
                CallsDownstreamApi = application.RequiredResourceAccess.Any(r => r.ResourceAppId != MicrosoftGraphAppId),
                LogoutUrl = application.Web?.LogoutUrl,

                // Parameters that cannot be infered from the registered app
                SusiPolicy = originalApplicationParameters.SusiPolicy,
                SecretsId = originalApplicationParameters.SecretsId,
                TargetFramework = originalApplicationParameters.TargetFramework,
                MsalAuthenticationOptions = originalApplicationParameters.MsalAuthenticationOptions,
                CalledApiScopes = originalApplicationParameters.CalledApiScopes,
            };

            // Todo: might be a bit more complex in some cases for the B2C case.
            // TODO: introduce the Instance?
            effectiveApplicationParameters.Authority = isB2C
                 ? $"https://{effectiveApplicationParameters.Domain1}.b2clogin.com/{effectiveApplicationParameters.Domain}/{effectiveApplicationParameters.SusiPolicy}/"
                 : $"https://login.microsoftonline.com/{effectiveApplicationParameters.TenantId ?? effectiveApplicationParameters.Domain}/";
            effectiveApplicationParameters.Instance = isB2C
                ? $"https://{effectiveApplicationParameters.Domain1}.b2clogin.com/"
                : originalApplicationParameters.Instance;

            effectiveApplicationParameters.PasswordCredentials.AddRange(application.PasswordCredentials.Select(p => p.Hint + "******************"));
            if (application.Web != null && application.Web.RedirectUris != null)
            {
                effectiveApplicationParameters.WebRedirectUris.AddRange(application.Web.RedirectUris);
            }
            effectiveApplicationParameters.SignInAudience = MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(effectiveApplicationParameters.SignInAudience!);
            return effectiveApplicationParameters;
        }
    }
}
