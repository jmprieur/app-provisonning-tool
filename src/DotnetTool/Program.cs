using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetTool
{
    class Program
    {
        /// <summary>
        /// Mapping between the command line and the ProvisionningToolOptions
        /// </summary>
        private static Dictionary<string, string> mapping = new Dictionary<string, string>
            {
                {"--folder", nameof(ProvisioningToolOptions.CodeFolder)},
                {"--language-framework", nameof(ProvisioningToolOptions.LanguageOrFramework)},
                {"--username", nameof(ProvisioningToolOptions.Username)},
                {"--client-secret", nameof(ProvisioningToolOptions.ClientSecret)},
                {"--client-id", nameof(ProvisioningToolOptions.ClientId)},
                {"--tenant-id", nameof(ProvisioningToolOptions.TenantId)},
            };
   
        static async Task Main(string[] args)
        {
            // Read options
            ProvisioningToolOptions provisioningToolOptions = GetOptions(args);
            if (string.IsNullOrEmpty(provisioningToolOptions.CodeFolder))
            {
                provisioningToolOptions.CodeFolder = System.IO.Directory.GetCurrentDirectory();
            }

            AppProvisionningTool appProvisionningTool = new AppProvisionningTool(provisioningToolOptions);
            await appProvisionningTool.Run();
        }

        /// <summary>
        /// Get the options from the command line
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Provisionning tool options</returns>
        private static ProvisioningToolOptions GetOptions(string[] args)
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddCommandLine(args, mapping);
            IConfiguration configuration = configurationBuilder.Build();
            ProvisioningToolOptions provisioningToolOptions = new ProvisioningToolOptions();
            ConfigurationBinder.Bind(configuration, provisioningToolOptions);

            string? projectType = args.FirstOrDefault(arg => !arg.StartsWith("--"));
            bool help = args.Any(arg => !arg.StartsWith("--help"));

            provisioningToolOptions.ProjectType = projectType;
            provisioningToolOptions.Help = help;

            return provisioningToolOptions;
        }
    }
}
