using Azure.Core;
using DotnetTool.AuthenticationParameters;
using System;

namespace DotnetTool
{
    public class AppProvisionningTool
    {
        private ProvisioningToolOptions provisioningToolOptions { get; set; }

        public AppProvisionningTool(ProvisioningToolOptions provisioningToolOptions)
        {
            this.provisioningToolOptions = provisioningToolOptions;
        }

        internal void Run()
        {

            // If needed, infer project type from code
            ProjectDescription projectDescription = InferProjectDescription(
                provisioningToolOptions.ProjectTypeIdentifier,
                provisioningToolOptions.CodeFolder);

            ApplicationParameters applicationParameters = InferApplicationParameters(
                provisioningToolOptions, projectDescription) ;

            // Get developer credentials
            TokenCredential tokenCredential = GetTokenCredential(provisioningToolOptions);

            // Read or provision Microsoft identity platform application
            ApplicationParameters effectiveApplicationParameters = ReadOrProvisionMicrosoftIdentityApplication(
                tokenCredential, 
                applicationParameters);

            // Reconciliate code configuration and app registration
            ApplicationParameters reconcialedApplicationParameters = Reconciliate(
                applicationParameters, 
                effectiveApplicationParameters);

            // Write code configuration and/or app registration
            Summary summary = new Summary();
            WriteProjectConfiguration(
                summary, 
                reconcialedApplicationParameters, 
                projectDescription);
            WriteApplictionRegistration(
                summary, 
                reconcialedApplicationParameters);

            // Summarizes what happened
            WriteSummary(summary);
        }

        private void WriteSummary(Summary summary)
        {
            Console.WriteLine("Summary");
        }

        private void WriteApplictionRegistration(Summary summary, ApplicationParameters reconcialedApplicationParameters)
        {
            Console.WriteLine(nameof(WriteApplictionRegistration));
        }

        private void WriteProjectConfiguration(Summary summary, ApplicationParameters reconcialedApplicationParameters, ProjectDescription projectDescription)
        {
            Console.WriteLine(nameof(WriteProjectConfiguration));
        }

        private ApplicationParameters Reconciliate(ApplicationParameters applicationParameters, ApplicationParameters effectiveApplicationParameters)
        {
            Console.WriteLine(nameof(Reconciliate));
            return applicationParameters;
        }

        private ApplicationParameters ReadOrProvisionMicrosoftIdentityApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            Console.WriteLine(nameof(Reconciliate));
            return null;
        }

        private ApplicationParameters InferApplicationParameters(ProvisioningToolOptions provisioningToolOptions, ProjectDescription projectDescription)
        {
            Console.WriteLine(nameof(InferApplicationParameters));
            return null;
        }

        private TokenCredential GetTokenCredential(ProvisioningToolOptions provisioningToolOptions)
        {
            Console.WriteLine(nameof(GetTokenCredential));
            return null;
        }

        private ProjectDescription InferProjectDescription(string projectTypeIdentifier, string codeFolder)
        {
            Console.WriteLine(nameof(InferProjectDescription));
            return null;
        }
    }
}