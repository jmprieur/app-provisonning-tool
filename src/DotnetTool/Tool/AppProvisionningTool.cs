using Azure.Core;
using DotnetTool.AuthenticationParameters;
using DotnetTool.CodeReaderWriter;
using DotnetTool.DeveloperCredentials;
using DotnetTool.MicrosoftIdentityPlatformApplication;
using DotnetTool.Project;
using System;
using System.Threading.Tasks;

namespace DotnetTool
{
    public class AppProvisionningTool
    {
        private ProvisioningToolOptions provisioningToolOptions { get; set; }

        private MicrosoftIdentityPlatformApplicationManager MicrosoftIdentityPlatformApplicationManager { get; } = new MicrosoftIdentityPlatformApplicationManager();

        public AppProvisionningTool(ProvisioningToolOptions provisioningToolOptions)
        {
            this.provisioningToolOptions = provisioningToolOptions;
        }

        internal async Task Run()
        {
            // If needed, infer project type from code
            ProjectDescription projectDescription = GetProjectDescription(
                provisioningToolOptions.ProjectTypeIdentifier,
                provisioningToolOptions.CodeFolder);

            ProjectAuthenticationSettings projectSettings = InferApplicationParameters(
                provisioningToolOptions, projectDescription) ;

            // Get developer credentials
            TokenCredential tokenCredential = GetTokenCredential(provisioningToolOptions);

            // Read or provision Microsoft identity platform application
            ApplicationParameters effectiveApplicationParameters = await ReadOrProvisionMicrosoftIdentityApplication(
                tokenCredential, 
                projectSettings.ApplicationParameters);

            // Reconciliate code configuration and app registration
            ApplicationParameters reconcialedApplicationParameters = Reconciliate(
                projectSettings.ApplicationParameters, 
                effectiveApplicationParameters);

            // Write code configuration and/or app registration
            Summary summary = new Summary();
            WriteProjectConfiguration(
                summary, 
                reconcialedApplicationParameters, 
                projectDescription);

            if (reconcialedApplicationParameters != effectiveApplicationParameters)
            {
                WriteApplicationRegistration(
                    summary,
                    reconcialedApplicationParameters);
            }

            // Summarizes what happened
            WriteSummary(summary);
        }

        private void WriteSummary(Summary summary)
        {
            Console.WriteLine("Summary");
        }

        private void WriteApplicationRegistration(Summary summary, ApplicationParameters reconcialedApplicationParameters)
        {
            Console.WriteLine(nameof(WriteApplicationRegistration));
        }

        private void WriteProjectConfiguration(Summary summary, ApplicationParameters reconcialedApplicationParameters, ProjectDescription projectDescription)
        {
            Console.WriteLine(nameof(WriteProjectConfiguration));
            summary.changes.Add(new Change() { Description = $"Writing the project AppId = {reconcialedApplicationParameters.ClientId}" });
        }

        private ApplicationParameters Reconciliate(ApplicationParameters applicationParameters, ApplicationParameters effectiveApplicationParameters)
        {
            Console.WriteLine(nameof(Reconciliate));
            return effectiveApplicationParameters;
        }

        private async Task<ApplicationParameters> ReadOrProvisionMicrosoftIdentityApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            Console.WriteLine(nameof(ReadOrProvisionMicrosoftIdentityApplication));
            ApplicationParameters currentApplicationParameters = MicrosoftIdentityPlatformApplicationManager.ReadApplication(tokenCredential, applicationParameters); 
            if (currentApplicationParameters == null)
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.CreateNewApp(tokenCredential, applicationParameters);
            }
            return currentApplicationParameters;
        }

        private ProjectAuthenticationSettings InferApplicationParameters(ProvisioningToolOptions provisioningToolOptions, ProjectDescription projectDescription)
        {
            Console.WriteLine(nameof(InferApplicationParameters));

            CodeReader reader = new CodeReader();
            ProjectAuthenticationSettings projectSettings = reader.ReadFromFiles(provisioningToolOptions.CodeFolder, projectDescription);
            return projectSettings;
        }

        public ProjectDescription GetProjectDescription(string projectTypeIdentifier, string codeFolder)
        {
            ProjectDescriptionReader projectDescriptionReader = new ProjectDescriptionReader();
            return projectDescriptionReader.GetProjectDescription(projectTypeIdentifier, codeFolder);
        }

        private TokenCredential GetTokenCredential(ProvisioningToolOptions provisioningToolOptions)
        {
            DeveloperCredentialsReader developerCredentialsReader = new DeveloperCredentialsReader();
            return developerCredentialsReader.GetDeveloperCredentials(provisioningToolOptions);
        }


    }
}