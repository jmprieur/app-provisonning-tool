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
            ProjectAuthenticationSettings projectAuthenticationSettings = new ProjectAuthenticationSettings();
            ProcessProject(folderToConfigure, projectDescription, projectAuthenticationSettings, projectDescriptions);
            return projectAuthenticationSettings;
        }

        private static void ProcessProject(string folderToConfigure, ProjectDescription projectDescription, ProjectAuthenticationSettings projectAuthenticationSettings, IEnumerable<ProjectDescription> projectDescriptions)
        {
            string projectPath = Path.Combine(folderToConfigure, projectDescription.ProjectRelativeFolder!);

            // Do DO get all the project descriptions
            foreach (ConfigurationProperties configurationProperties in projectDescription.GetMergedFiles(projectDescriptions))
            {
                    string filePath = Path.Combine(projectPath, configurationProperties.FileRelativePath!).Replace('/', '\\');
                    ProcessFile(projectAuthenticationSettings, filePath, configurationProperties);
            }
        }

        private static void ProcessFile(ProjectAuthenticationSettings projectAuthenticationSettings, string filePath, ConfigurationProperties file)
        {
            Console.WriteLine($"{filePath}");

            if (filePath.EndsWith(".json") && File.Exists(filePath))
            {
                string fileContent = System.IO.File.ReadAllText(filePath);
                JsonElement jsonContent = JsonSerializer.Deserialize<JsonElement>(fileContent,
                                                                                  serializerOptionsWithComments);

                foreach (PropertyMapping propertyMapping in file.Properties)
                {
                    string property = propertyMapping.Property!; // Valid
                    string[] path = property.Split(':');

                    JsonElement element = jsonContent;

                    bool found = true;
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

                            if (!string.IsNullOrEmpty(propertyMapping.Sets))
                            {
                                switch (propertyMapping.Sets)
                                {
                                    case "IsAad":
                                        projectAuthenticationSettings.ApplicationParameters.IsAAD = true;
                                        break;
                                    case "IsB2C":
                                        projectAuthenticationSettings.ApplicationParameters.IsB2C = true;
                                        break;
                                }
                            }
                        }
                    }
                    // TODO: else AddNotFound?
                }
            }
        }

        private static void ReadCodeSetting(string represents, string value, ProjectAuthenticationSettings projectAuthenticationSettings)
        {
            switch(represents)
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
