using DotnetTool.AuthenticationParameters;
using DotnetTool.Project;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using ConfigurationProperties = DotnetTool.Project.ConfigurationProperties;

namespace DotnetTool.CodeReaderWriter
{
    public class CodeReader
    {
        static JsonSerializerOptions serializerOptionsWithComments = new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public ProjectAuthenticationSettings ReadFromFiles(string folderToConfigure, ProjectDescription projectDescription, IEnumerable<ProjectDescription> projectDescriptions)
        {
            ProjectAuthenticationSettings projectAuthenticationSettings = new ProjectAuthenticationSettings(projectDescription);
            ProcessProject(folderToConfigure, projectDescription, projectAuthenticationSettings, projectDescriptions);
            return projectAuthenticationSettings;
        }

        private static void ProcessProject(string folderToConfigure, ProjectDescription projectDescription, ProjectAuthenticationSettings projectAuthenticationSettings, IEnumerable<ProjectDescription> projectDescriptions)
        {
            string projectPath = Path.Combine(folderToConfigure, projectDescription.ProjectRelativeFolder!);

            // Do DO get all the project descriptions
            var properties = projectDescription.GetMergedFiles(projectDescriptions).ToArray();
            foreach (ConfigurationProperties configurationProperties in properties)
            {
                string filePath = Path.Combine(projectPath, configurationProperties.FileRelativePath!).Replace('/', '\\');
                ProcessFile(projectAuthenticationSettings, filePath, configurationProperties);
            }
        }

        private static void ProcessFile(ProjectAuthenticationSettings projectAuthenticationSettings, string filePath, ConfigurationProperties file)
        {
            Console.WriteLine($"{filePath}");

            if (File.Exists(filePath))
            {
                string fileContent = System.IO.File.ReadAllText(filePath);
                JsonElement jsonContent = default(JsonElement);

                if (filePath.EndsWith(".json"))
                {
                    jsonContent = JsonSerializer.Deserialize<JsonElement>(fileContent,
                                                                                  serializerOptionsWithComments);
                }

                foreach (PropertyMapping propertyMapping in file.Properties)
                {
                    bool found = false;
                    string property = propertyMapping.Property;
                    if (property != null)
                    {
                        string[] path = property.Split(':');

                        JsonElement element = jsonContent;
                        found = true;
                        foreach (string segment in path)
                        {
                            JsonProperty prop = element.EnumerateObject().FirstOrDefault(e => e.Name == segment);
                            if (prop.Value.ValueKind == JsonValueKind.Undefined)
                            {
                                found = false;
                                break;
                            }
                            element = prop.Value;
                        }

                        if (found)
                        {
                            string replaceFrom = element.ValueKind == JsonValueKind.Number ? element.GetInt32().ToString(CultureInfo.InvariantCulture) : element.ToString();

                            if (!string.IsNullOrEmpty(propertyMapping.Represents))
                            {
                                ReadCodeSetting(propertyMapping.Represents, replaceFrom, projectAuthenticationSettings);

                                int index = GetIndex(element);
                                int length = replaceFrom.Length;

                                AddReplacement(projectAuthenticationSettings, filePath, index, length, replaceFrom, propertyMapping.Represents);
                            }


                        }
                    }

                    if (!string.IsNullOrEmpty(propertyMapping.Sets))
                    {
                        if (found 
                            || (propertyMapping.MatchAny != null && propertyMapping.MatchAny.Any(m => fileContent.Contains(m))))
                        {
                            projectAuthenticationSettings.ApplicationParameters.Sets(propertyMapping.Sets);
                        }
                    }
                }
                // TODO: else AddNotFound?
            }
        }

        private static void ReadCodeSetting(string represents, string value, ProjectAuthenticationSettings projectAuthenticationSettings)
        {
            switch (represents)
            {
                case "Application.ClientId":
                    projectAuthenticationSettings.ApplicationParameters.ClientId = value;
                    break;
                case "Directory.TenantId":
                    projectAuthenticationSettings.ApplicationParameters.TenantId = value;
                    break;
            }
        }

        private static int GetIndex(JsonElement element)
        {
            Type type = element.GetType()!;
            object _idx = type.GetField("_idx",
                                        BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(element)!;
            return (int)_idx;
        }

        private static void AddReplacement(ProjectAuthenticationSettings projectAuthenticationSettings, string filePath, int index, int length, string replaceFrom, string replaceBy)
        {
            projectAuthenticationSettings.Replacements.Add(new Replacement(filePath, index, length, replaceFrom, replaceBy));
        }

    }
}
