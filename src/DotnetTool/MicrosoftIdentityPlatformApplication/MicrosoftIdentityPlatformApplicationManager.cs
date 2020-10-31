using Azure.Core;
using DotnetTool.AuthenticationParameters;
using DotnetTool.DeveloperCredentials;
using Microsoft.Graph;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetTool.MicrosoftIdentityPlatformApplication
{
    public class MicrosoftIdentityPlatformApplicationManager
    {
        GraphServiceClient graphServiceClient;

        internal async Task<ApplicationParameters> CreateNewApp(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            GetGraphServiceClient(tokenCredential);

            Application application = new Application()
            {
                DisplayName = applicationParameters.DisplayName,
                SignInAudience = AppParameterAudienceToMicrosoftIdentityPlatformAppAudience(applicationParameters.Audience),
                Description = applicationParameters.Description
            };

            if (applicationParameters.Platforms.Any(p => p.Type == PlatformType.Web))
            {
                // TODO: do for Web APIs
                application.Api = new ApiApplication()
                {
                     RequestedAccessTokenVersion = 2,
                };
            }

            Application createdApplication = await graphServiceClient.Applications
                .Request()
                .AddAsync(application);

            var effectiveApplicationParameters = new ApplicationParameters
            {
                DisplayName = createdApplication.DisplayName,
                ClientId = createdApplication.AppId,
            };

            effectiveApplicationParameters.Audience = MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(effectiveApplicationParameters.Audience);

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
                    return null;
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
            if (graphServiceClient == null)
            {
                graphServiceClient = new GraphServiceClient(new TokenCredentialAuthenticationProvider(tokenCredential));
            }
            return graphServiceClient;
        }

        internal async Task<ApplicationParameters> ReadApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            GetGraphServiceClient(tokenCredential);

            var apps = await graphServiceClient.Applications
                .Request()
                .Filter($"appId eq '{applicationParameters.ClientId}'")
                .GetAsync();

            var createdApplication = apps.FirstOrDefault();

            if (createdApplication == null)
            {
                // we should not be here: application not found in the tenant
            }

            var effectiveApplicationParameters = new ApplicationParameters
            {
                DisplayName = createdApplication.DisplayName,
                ClientId = createdApplication.AppId,
                // TODO: parse the platforms
            };

            effectiveApplicationParameters.Audience = MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(effectiveApplicationParameters.Audience);

            return effectiveApplicationParameters;

        }
    }
}
