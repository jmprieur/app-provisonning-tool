using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetTool
{
    public static class Program
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
