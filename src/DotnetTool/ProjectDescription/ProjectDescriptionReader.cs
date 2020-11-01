using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DotnetTool.Project
{
    public class ProjectDescriptionReader
    {
        public List<ProjectDescription> projectDescriptions { get; private set; } = new List<ProjectDescription>();

        public ProjectDescription GetProjectDescription(string projectTypeIdentifier, string codeFolder)
        {
            string projectTypeId = projectTypeIdentifier;
            if (string.IsNullOrEmpty(projectTypeId) || projectTypeId == "dotnet-")
            {
                projectTypeId = InferProjectType(codeFolder);
            }

            return ReadProjectDescription(projectTypeId);
        }

        private ProjectDescription ReadProjectDescription(string projectTypeIdentifier)
        {
            ReadProjectDescriptions();

            return projectDescriptions.FirstOrDefault(projectDescription => projectDescription.Identifier == projectTypeIdentifier);
        }

        static JsonSerializerOptions serializerOptionsWithComments = new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private ProjectDescription ReadDescriptionFromFileContent(byte[] fileContent)
        {
            string jsonText = Encoding.UTF8.GetString(fileContent);
            return JsonSerializer.Deserialize<ProjectDescription>(jsonText, serializerOptionsWithComments);
        }

        private string InferProjectType(string codeFolder)
        {
            ReadProjectDescriptions();

            foreach(ProjectDescription projectDescription in projectDescriptions.Where(p => p.MatchesForProjectType!=null))
            {
                foreach(MatchesForProjectType matchesForProjectType in projectDescription.MatchesForProjectType)
                {
                    string filePath = Path.Combine(codeFolder, matchesForProjectType.FileRelativePath);
                    string fileContent = File.ReadAllText(filePath);
                    foreach(string match in matchesForProjectType.MatchAny)
                    {
                        if (fileContent.Contains(match))
                        {
                            return projectDescription.Identifier;
                        }
                    }
                }
            }
            return null;
        }

        private void ReadProjectDescriptions()
        {
            if (projectDescriptions.Any())
            {
                return;
            }

            var properties = typeof(Properties.Resources).GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(p => p.PropertyType == typeof(byte[]))
                .ToArray();
            foreach (PropertyInfo propertyInfo in properties)
            {
                var content = propertyInfo.GetValue(null) as byte[];
                ProjectDescription projectDescription = ReadDescriptionFromFileContent(content);
                projectDescriptions.Add(projectDescription);
            }

            // TODO: provide an extension mechanism to add such files outside the tool.
        }
    }

}