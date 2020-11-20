﻿
using DotnetTool;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{

    public class E2ETests
    {
        private readonly ITestOutputHelper testOutput;

        public E2ETests(ITestOutputHelper output)
        {
            this.testOutput = output;
        }

        //[InlineData("webapp2\\webapp2-noauth", "dotnet new webapp2")]
        [InlineData("webapp2\\webapp2-singleorg", "dotnet new webapp --auth SingleOrg")]
        //[InlineData("webapp2\\webapp2-singleorg-callsgraph", "dotnet new webapp2 --auth SingleOrg --calls-graph")]
        //[InlineData("webapp2\\webapp2-singleorg-callswebapi", "dotnet new webapp2 --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        [InlineData("webapp2\\webapp2-b2c", "dotnet new webapp --auth IndividualB2C")]
        //[InlineData("webapp2\\webapp2-b2c-callswebapi", "dotnet new webapp2 --auth IndividualB2C --called-api-url \"https://localhost:44332/api/todolist\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/tasks/read\"")]
        //[InlineData("webapi2\\webapi2-noauth", "dotnet new webapi2")]
        [InlineData("webapi2\\webapi2-singleorg", "dotnet new webapi --auth SingleOrg")]
        //[InlineData("webapi2\\webapi2-singleorg-callsgraph", "dotnet new webapi2 --auth SingleOrg --calls-graph")]
        //[InlineData("webapi2\\webapi2-singleorg-callswebapi", "dotnet new webapi2 --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        [InlineData("webapi2\\webapi2-b2c", "dotnet new webapi --auth IndividualB2C")]
        //[InlineData("webapi2\\webapi2-b2c-callswebapi", "dotnet new webapi2 --auth IndividualB2C --called-api-url \"https://localhost:44332/api/todolist\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/tasks/read\"")]
        //[InlineData("mvc2\\mvc2-noauth", "dotnet new mvc2")]
        [InlineData("mvc2\\mvc2-singleorg", "dotnet new mvc --auth SingleOrg")]
        //[InlineData("mvc2\\mvc2-singleorg-callsgraph", "dotnet new mvc2 --auth SingleOrg --calls-graph")]
        //[InlineData("mvc2\\mvc2-singleorg-callswebapi", "dotnet new mvc2 --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        [InlineData("mvc2\\mvc2-b2c", "dotnet new mvc --auth IndividualB2C")]
        //[InlineData("mvc2\\mvc2-b2c-callswebapi", "dotnet new mvc2 --auth IndividualB2C --called-api-url \"https://localhost:44332/api/todolist\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/tasks/read\"")]
        //[InlineData("blazorserver2\\blazorserver2-noauth", "dotnet new blazorserver2")]
        //[InlineData("blazorserver2\\blazorserver2-singleorg", "dotnet new blazorserver2 --auth SingleOrg")]
        //[InlineData("blazorserver2\\blazorserver2-singleorg-callsgraph", "dotnet new blazorserver2 --auth SingleOrg --calls-graph")]
        //[InlineData("blazorserver2\\blazorserver2-singleorg-callswebapi", "dotnet new blazorserver2 --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        //[InlineData("blazorserver2\\blazorserver2-b2c", "dotnet new blazorserver2 --auth IndividualB2C")]
        //[InlineData("blazorserver2\\blazorserver2-b2c-callswebapi", "dotnet new blazorserver2 --auth IndividualB2C --called-api-url \"https://localhost:44332/api/todolist\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/tasks/read\"")]
        //[InlineData("blazorwasm2\\blazorwasm2-noauth", "dotnet new blazorwasm2")]
        //[InlineData("blazorwasm2\\blazorwasm2-singleorg", "dotnet new blazorwasm2 --auth SingleOrg")]
        //[InlineData("blazorwasm2\\blazorwasm2-singleorg-callsgraph", "dotnet new blazorwasm2 --auth SingleOrg --calls-graph")]
        //[InlineData("blazorwasm2\\blazorwasm2-singleorg-callswebapi", "dotnet new blazorwasm2 --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        //[InlineData("blazorwasm2\\blazorwasm2-singleorg-hosted", "dotnet new blazorwasm2 --auth SingleOrg  --hosted")]
        //[InlineData("blazorwasm2\\blazorwasm2-singleorg-callsgraph-hosted", "dotnet new blazorwasm2 --auth SingleOrg --calls-graph --hosted")]
        //[InlineData("blazorwasm2\\blazorwasm2-singleorg-callswebapi-hosted", "dotnet new blazorwasm2 --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\" --hosted")]
        //[InlineData("blazorwasm2\\blazorwasm2-b2c", "dotnet new blazorwasm2 --auth IndividualB2C")]
        //[InlineData("blazorwasm2\\blazorwasm2-b2c-hosted", "dotnet new blazorwasm2 --auth IndividualB2C  --hosted")]
        //[InlineData("blazorwasm2\\blazorwasm2-b2c-callswebapi-hosted", "dotnet new blazorwasm2 --auth IndividualB2C --called-api-url \"https://localhost:44332/api/todolist\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/tasks/read\" --hosted")]
        [Theory]
        public async Task TestEndToEnd(string folder, string command)
        {
            // Create the folder
            string executionFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string folderToCreate = Path.Combine(executionFolder, "Tests", folder);
            Directory.CreateDirectory(folderToCreate);
            ProcessStartInfo processStartInfo = new ProcessStartInfo("dotnet", command.Replace("dotnet ", string.Empty) + " --force");
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            Environment.GetEnvironmentVariables();
            processStartInfo.WorkingDirectory = folderToCreate;
            Process? process = Process.Start(processStartInfo);
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            testOutput.WriteLine(output);
            string errors = process.StandardError.ReadToEnd();
            testOutput.WriteLine(errors);
            Assert.Equal(string.Empty, errors);

            string currentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(folderToCreate);
                ProvisioningToolOptions provisioningToolOptions = new ProvisioningToolOptions();
                if (folder.Contains("b2c"))
                {
                    provisioningToolOptions.TenantId = "fabrikamb2c.onmicrosoft.com";
                }
                AppProvisionningTool appProvisionningTool = new AppProvisionningTool(provisioningToolOptions);
                await appProvisionningTool.Run();
            }
            catch (Exception ex)
            {
                testOutput.WriteLine(ex.ToString());
                Assert.True(false);
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }

            testOutput.WriteLine($"See {folderToCreate}");
        }
    }
}
