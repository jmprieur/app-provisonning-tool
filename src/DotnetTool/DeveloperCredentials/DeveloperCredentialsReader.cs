using Azure.Core;
using Azure.Identity;

namespace DotnetTool.DeveloperCredentials
{
    public class DeveloperCredentialsReader
    {
        internal TokenCredential GetDeveloperCredentials(IDeveloperCredentialsOptions provisioningToolOptions)
        {
            DefaultAzureCredentialOptions defaultAzureCredentialOptions = new DefaultAzureCredentialOptions()
            {
                SharedTokenCacheTenantId = provisioningToolOptions.TenantId,
                SharedTokenCacheUsername = provisioningToolOptions.Username
            };
            defaultAzureCredentialOptions.ExcludeManagedIdentityCredential = true;
            defaultAzureCredentialOptions.ExcludeInteractiveBrowserCredential = false;

            DefaultAzureCredential credential = new DefaultAzureCredential(defaultAzureCredentialOptions);
            return credential;
        }
    }
}
