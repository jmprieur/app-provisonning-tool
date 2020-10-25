using Azure.Core;
using DotnetTool.AuthenticationParameters;
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
                case "MultiOrgAndPersonal":
                    return "AADMSA";
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
                case "AADMSA":
                default:
                    return "MultiOrgAndPersonal";
            }
        }

        private GraphServiceClient GetGraphServiceClient(TokenCredential tokenCredential)
        {
            if (graphServiceClient == null)
            {
                graphServiceClient = new GraphServiceClient(new TokenCredentialCredentialProvider(tokenCredential));
            }
            return graphServiceClient;
        }

        internal ApplicationParameters ReadApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            // TODO
            return null;
        }
    }
}
