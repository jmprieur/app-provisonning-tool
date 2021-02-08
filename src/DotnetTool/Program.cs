using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetTool
{
    public static class Program
    {
        /// <summary>
        /// Creates or updates an AzureAD/Azure AD B2C application, and updates the code, using
        /// the developer credentials (Visual Studio, Azure CLI, Azure RM PowerShell, VS Code)
        /// </summary>
        /// <param name="tenantId">Azure AD or Azure AD B2C tenant in which to create/update the app. By default
        /// this will be your home tenant ID</param>
        /// <param name="username">Username to use to connect to the Azure AD or Azure AD B2C tenant.
        /// By default this will be your home user id</param>
        /// <param name="folder">Folder in which to look at the code. By default the current folder</param>
        /// <param name="clientId">Client ID of an existing application from which to update the code.</param>
        /// <param name="clientSecret">Client secret to use as a client credential</param>
        /// <param name="unregister">Unregister the application, instead of registering it</param>
        /// <returns></returns>
        static public async Task Main(
            string tenantId = null,
            string username = null,
            string clientId = null,
            bool unregister = false,
            string folder = null,
            string clientSecret = null)
        {
            // Read options
            ProvisioningToolOptions provisioningToolOptions = new ProvisioningToolOptions
            {
                Username = username,
                ClientId = clientId,
                ClientSecret = clientSecret,
                TenantId = tenantId
            };
            if (folder!=null)
            {
                provisioningToolOptions.CodeFolder = folder;
            }

            AppProvisionningTool appProvisionningTool = new AppProvisionningTool(provisioningToolOptions);
            await appProvisionningTool.Run();
        }

        public static void GenerateTests()
        {
            string parentFolder = @"C:\gh\microsoft-identity-web\ProjectTemplates\bin\Debug\tests";

            foreach (string subFolder in System.IO.Directory.GetDirectories(parentFolder))
            {
                foreach (string projectFolder in System.IO.Directory.GetDirectories(subFolder))
                {
                    System.Console.WriteLine($"[InlineData(@\"{System.IO.Path.GetFileName(subFolder)}\\{System.IO.Path.GetFileName(projectFolder)}\", {projectFolder.Contains("b2c")}, \"dotnet-WebApp\")]");
                }
            }
        }
    }
}
