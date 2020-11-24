using Azure.Core;
using DotnetTool.AuthenticationParameters;
using DotnetTool.CodeReaderWriter;
using DotnetTool.DeveloperCredentials;
using DotnetTool.MicrosoftIdentityPlatformApplication;
using DotnetTool.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace DotnetTool
{
    public class AppProvisionningTool
    {
        private ProvisioningToolOptions provisioningToolOptions { get; set; }

        private MicrosoftIdentityPlatformApplicationManager MicrosoftIdentityPlatformApplicationManager { get; } = new MicrosoftIdentityPlatformApplicationManager();

        private ProjectDescriptionReader projectDescriptionReader { get; } = new ProjectDescriptionReader();

        public AppProvisionningTool(ProvisioningToolOptions provisioningToolOptions)
        {
            this.provisioningToolOptions = provisioningToolOptions;
        }

        public async Task Run()
        {
            // If needed, infer project type from code
            ProjectDescription projectDescription = projectDescriptionReader.GetProjectDescription(
                provisioningToolOptions.ProjectTypeIdentifier,
                provisioningToolOptions.CodeFolder);

            if (projectDescription == null)
            {
                Console.WriteLine("Unknown project type");
                throw new Exception();
            }

            ProjectAuthenticationSettings projectSettings = InferApplicationParameters(
                provisioningToolOptions,
                projectDescription,
                projectDescriptionReader.projectDescriptions);

            // Get developer credentials
            TokenCredential tokenCredential = GetTokenCredential(
                provisioningToolOptions,
                projectSettings.ApplicationParameters.TenantId ?? projectSettings.ApplicationParameters.Domain);

            if (provisioningToolOptions.Unregister)
            {
                await UnregisterApplication(tokenCredential, projectSettings.ApplicationParameters);
                return;
            }

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
                projectSettings,
                reconcialedApplicationParameters);

            if (reconcialedApplicationParameters != effectiveApplicationParameters)
            {
                WriteApplicationRegistration(
                    summary,
                    reconcialedApplicationParameters,
                    tokenCredential);
            }

            // Summarizes what happened
            WriteSummary(summary);
        }

        private void WriteSummary(Summary summary)
        {
            Console.WriteLine("Summary");
            foreach(Change change in summary.changes)
            {
                Console.WriteLine($"{change.Description}");
            }
        }

        private void WriteApplicationRegistration(Summary summary, ApplicationParameters reconcialedApplicationParameters, TokenCredential tokenCredential)
        {
            summary.changes.Add(new Change($"Writing the project AppId = {reconcialedApplicationParameters.ClientId}"));
        }

        private void WriteProjectConfiguration(Summary summary, ProjectAuthenticationSettings projectSettings, ApplicationParameters reconcialedApplicationParameters)
        {
            CodeWriter codeWriter = new CodeWriter();
            codeWriter.WriteConfiguration(summary, projectSettings.Replacements, reconcialedApplicationParameters);
        }

        private ApplicationParameters Reconciliate(ApplicationParameters applicationParameters, ApplicationParameters effectiveApplicationParameters)
        {
            Console.WriteLine(nameof(Reconciliate));
            return effectiveApplicationParameters;
        }

        private async Task<ApplicationParameters> ReadOrProvisionMicrosoftIdentityApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            ApplicationParameters? currentApplicationParameters = null;
            if (!string.IsNullOrEmpty(applicationParameters.ClientId))
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.ReadApplication(tokenCredential, applicationParameters);
                if (currentApplicationParameters == null)
                {
                    Console.Write($"Couldn't find app {applicationParameters.ClientId} in tenant {applicationParameters.TenantId}");
                }
            }

            if (currentApplicationParameters == null && !provisioningToolOptions.Unregister)
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.CreateNewApp(tokenCredential, applicationParameters);
                currentApplicationParameters.SecretsId = applicationParameters.SecretsId;
                Console.Write($"Created app {currentApplicationParameters.ClientId}");
            }
            return currentApplicationParameters;
        }

        private ProjectAuthenticationSettings InferApplicationParameters(
            ProvisioningToolOptions provisioningToolOptions, 
            ProjectDescription projectDescription,
            IEnumerable<ProjectDescription> projectDescriptions)
        {
            CodeReader reader = new CodeReader();
            ProjectAuthenticationSettings projectSettings = reader.ReadFromFiles(provisioningToolOptions.CodeFolder, projectDescription, projectDescriptions);
            projectSettings.ApplicationParameters.DisplayName ??= Path.GetFileName(provisioningToolOptions.CodeFolder);
            return projectSettings;
        }


        private TokenCredential GetTokenCredential(ProvisioningToolOptions provisioningToolOptions, string? currentApplicationTenantId)
        {
            DeveloperCredentialsReader developerCredentialsReader = new DeveloperCredentialsReader();
            return developerCredentialsReader.GetDeveloperCredentials(
                provisioningToolOptions.Username, 
                currentApplicationTenantId ?? provisioningToolOptions.TenantId);
        }

        private async Task UnregisterApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            await MicrosoftIdentityPlatformApplicationManager.Unregister(tokenCredential, applicationParameters);
        }
    }
}