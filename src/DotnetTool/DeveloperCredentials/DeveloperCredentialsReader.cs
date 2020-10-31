using Azure.Core;
using Azure.Identity;
using DotnetTool.MicrosoftIdentityPlatformApplication;

namespace DotnetTool.DeveloperCredentials
{
    public class DeveloperCredentialsReader
    {
        internal TokenCredential GetDeveloperCredentials(IDeveloperCredentialsOptions provisioningToolOptions, string currentApplicationTenantId)
        {
            /*
             * Tried but does not work if another tenant than the home tenant id is specified
                        DefaultAzureCredentialOptions defaultAzureCredentialOptions = new DefaultAzureCredentialOptions()
                        {
                            SharedTokenCacheTenantId = provisioningToolOptions.TenantId ?? currentApplicationTenantId,
                            SharedTokenCacheUsername = provisioningToolOptions.Username,
                        };
                        defaultAzureCredentialOptions.ExcludeManagedIdentityCredential = true;
                        defaultAzureCredentialOptions.ExcludeInteractiveBrowserCredential = true;
                        defaultAzureCredentialOptions.ExcludeAzureCliCredential = true;
                        defaultAzureCredentialOptions.ExcludeEnvironmentCredential = true;



                        DefaultAzureCredential credential = new DefaultAzureCredential(defaultAzureCredentialOptions);
                        return credential;
            */
            TokenCredential tokenCredential = new MsalTokenCredential(
                currentApplicationTenantId,
                provisioningToolOptions.Username);
            return tokenCredential;
        }
    }
}
