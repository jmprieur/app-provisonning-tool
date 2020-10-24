using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetTool
{
    class Program
    {
        private static Dictionary<string, string> mapping = new Dictionary<string, string>
            {
                {"--folder", nameof(ProvisioningToolOptions.CodeFolder)},
                {"--language-framework", nameof(ProvisioningToolOptions.LanguageOrFramework)},
                {"--app-owner", nameof(ProvisioningToolOptions.Username)},
                {"--client-secret", nameof(ProvisioningToolOptions.ClientSecret)},
                {"--client-id", nameof(ProvisioningToolOptions.ClientId)},
                {"--tenant-id", nameof(ProvisioningToolOptions.TenantId)},
            };
   
        static void Main(string[] args)
        {
            // Read options
            ProvisioningToolOptions provisioningToolOptions = GetOptions(args);

            AppProvisionningTool appProvisionningTool = new AppProvisionningTool(provisioningToolOptions);
            appProvisionningTool.Run();
        }

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
