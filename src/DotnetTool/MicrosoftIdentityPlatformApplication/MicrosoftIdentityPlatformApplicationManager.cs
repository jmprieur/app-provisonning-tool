using Azure.Core;
using DotnetTool.AuthenticationParameters;
using Microsoft.Graph;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetTool.MicrosoftIdentityPlatformApplication
{
    public class MicrosoftIdentityPlatformApplicationManager
    {
        GraphServiceClient? _graphServiceClient;

        internal async Task<ApplicationParameters> CreateNewApp(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            Application application = new Application()
            {
                DisplayName = applicationParameters.DisplayName,
                SignInAudience = AppParameterAudienceToMicrosoftIdentityPlatformAppAudience(applicationParameters.SignInAudience!),
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

            effectiveApplicationParameters.SignInAudience = MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(effectiveApplicationParameters.SignInAudience!);

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

        internal async Task<ApplicationParameters> ReadApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            var apps = await graphServiceClient.Applications
                .Request()
                .Filter($"appId eq '{applicationParameters.ClientId}'")
                .GetAsync();

            var createdApplication = apps.FirstOrDefault();

            if (createdApplication == null)
            {
                return null;
            }

            var effectiveApplicationParameters = new ApplicationParameters
            {
                DisplayName = createdApplication.DisplayName,
                ClientId = createdApplication.AppId,
                // TODO: parse the platforms
            };

            effectiveApplicationParameters.SignInAudience = MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(effectiveApplicationParameters.SignInAudience!);

            return effectiveApplicationParameters;

        }
    }
}
