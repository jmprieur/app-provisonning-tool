using Azure.Core;
using DotnetTool.AuthenticationParameters;
using Microsoft.Graph;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetTool.MicrosoftIdentityPlatformApplication
{
    public class MicrosoftIdentityPlatformApplicationManager
    {
        const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";
        private Guid ScopeOpenId = new Guid("37f7f235-527c-4136-accd-4a02d197296e");
        private Guid ScopeOfflineAccess = new Guid("7427e0e9-2fba-42fe-b0c0-848c9e6a8182");
        const string ScopeType = "Scope";

        GraphServiceClient? _graphServiceClient;

        internal async Task<ApplicationParameters> CreateNewApp(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            var tenant = (await graphServiceClient.Organization
                .Request()
                .GetAsync()).FirstOrDefault();

            Application application = new Application()
            {
                DisplayName = applicationParameters.DisplayName,
                SignInAudience = AppParameterAudienceToMicrosoftIdentityPlatformAppAudience(applicationParameters.SignInAudience!),
                Description = applicationParameters.Description
            };


            if (applicationParameters.IsWebApi)
            {
                // TODO: do for Web APIs
                application.Api = new ApiApplication()
                {
                    RequestedAccessTokenVersion = 2,
                };
            }

            if (applicationParameters.IsWebApp)
            {
                application.Web = new WebApplication();

                // IdToken
                if (!applicationParameters.CallsDownstreamApi && !applicationParameters.CallsMicrosoftGraph)
                {
                    application.Web.ImplicitGrantSettings = new ImplicitGrantSettings();
                    application.Web.ImplicitGrantSettings.EnableIdTokenIssuance = true;
                    if (applicationParameters.IsB2C)
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
                if (applicationParameters.IsB2C)
                {
                    application.RequiredResourceAccess = new RequiredResourceAccess[]
                    {
                    new RequiredResourceAccess
                    {
                         ResourceAppId = MicrosoftGraphAppId,
                          ResourceAccess = new ResourceAccess[]
                          {
                              // openid and offline_access
                               new ResourceAccess { Id = ScopeOpenId, Type = ScopeType},
                               new ResourceAccess { Id = ScopeOfflineAccess, Type = ScopeType},
                          }
                    }
                    };
                }
            }

            Application createdApplication = await graphServiceClient.Applications
                .Request()
                .AddAsync(application);

            // B2C does not allow user consent, and therefore we need to explicity create
            // a service principal and permission grants
            if (applicationParameters.IsB2C)
            {
                // Get the Microsoft Graph service principal
                var graphServicePrincipal = await graphServiceClient.ServicePrincipals
                    .Request()
                    .Filter($"AppId eq '{MicrosoftGraphAppId}'")
                    .GetAsync();

                // Creates a service principal (needed for B2C)
                ServicePrincipal servicePrincipal = new ServicePrincipal
                {
                    AppId = createdApplication.AppId,
                };

                var createdServicePrincipal = await graphServiceClient.ServicePrincipals
                    .Request()
                    .AddAsync(servicePrincipal);

                if (applicationParameters.IsWebApp)
                {
                    var oAuth2PermissionGrant = new OAuth2PermissionGrant
                    {
                        ClientId = createdServicePrincipal.Id,
                        ConsentType = "AllPrincipals",
                        PrincipalId = null,
                        ResourceId = graphServicePrincipal.FirstOrDefault().Id,
                        Scope = "openid offline_access"
                    };

                    await graphServiceClient.Oauth2PermissionGrants
                        .Request()
                        .AddAsync(oAuth2PermissionGrant);
                }
            }

            var effectiveApplicationParameters = GetEffectiveApplicationParameters(tenant, createdApplication);

            // Todo: can we move at creation?
            // Add a password credential. 
            if (applicationParameters.CallsMicrosoftGraph || applicationParameters.CallsDownstreamApi)
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


            return effectiveApplicationParameters;
        }

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

            ApplicationParameters effectiveApplicationParameters = GetEffectiveApplicationParameters(tenant, readApplication);

            return effectiveApplicationParameters;

        }

        private ApplicationParameters GetEffectiveApplicationParameters(Organization tenant, Application application)
        {
            var effectiveApplicationParameters = new ApplicationParameters
            {
                DisplayName = application.DisplayName,
                ClientId = application.AppId,
                IsAAD = (tenant.TenantType != "AAD B2C"),
                IsB2C = (tenant.TenantType == "AAD B2C"),
                HasAuthentication = true,
                IsWebApi = application.Api != null
                        && (application.Api.Oauth2PermissionScopes != null && application.Api.Oauth2PermissionScopes.Any())
                        || (application.AppRoles != null && application.AppRoles.Any()),
                IsWebApp = application.Web != null,
                TenantId = tenant.Id,
                Domain = tenant.VerifiedDomains.FirstOrDefault(v => v.IsDefault.HasValue && v.IsDefault.Value)?.Name,
                CallsMicrosoftGraph = application.RequiredResourceAccess.Any(r => r.ResourceAppId == MicrosoftGraphAppId),
                CallsDownstreamApi = application.RequiredResourceAccess.Any(r => r.ResourceAppId != MicrosoftGraphAppId),
                LogoutUrl = application.Web?.LogoutUrl,
            };

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
