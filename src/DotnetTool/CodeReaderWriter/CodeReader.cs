using DotnetTool.Project;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using File = DotnetTool.Project.File;

namespace DotnetTool.CodeReaderWriter
{
    public class CodeReader
    {
        static JsonSerializerOptions serializerOptionsWithComments = new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public ProjectAuthenticationSettings ReadFromFiles(string folderToConfigure, ProjectDescription projectDescription)
        {
            ProjectAuthenticationSettings projectAuthenticationSettings = new ProjectAuthenticationSettings();
            ProcessProject(folderToConfigure, projectDescription, projectAuthenticationSettings);
            return projectAuthenticationSettings;
        }

        private static void ProcessProject(string folderToConfigure, ProjectDescription projectDescription, ProjectAuthenticationSettings projectAuthenticationSettings)
        {
            string projectPath = Path.Combine(folderToConfigure, projectDescription.ProjectRelativeFolder);

            // Do DO get all the project descriptions
            foreach (File file in projectDescription.GetMergedFiles(new ProjectDescription[] { }))
            {
                string filePath = Path.Combine(projectPath, file.FileRelativePath).Replace('/', '\\');
                ProcessFile(projectAuthenticationSettings, filePath, file);
            }
        }

        private static void ProcessFile(ProjectAuthenticationSettings projectAuthenticationSettings, string filePath, File file)
        {
            Console.WriteLine($"{filePath}");

            if (filePath.EndsWith(".json"))
            {
                string fileContent = System.IO.File.ReadAllText(filePath);
                JsonElement jsonContent = JsonSerializer.Deserialize<JsonElement>(fileContent,
                                                                                  serializerOptionsWithComments);

                foreach (PropertyMapping propertyMapping in file.Properties)
                {
                    string[] path = propertyMapping.Property.Split(':');

                    JsonElement element = jsonContent;
                    foreach (string segment in path)
                    {
                        JsonProperty prop = element.EnumerateObject().FirstOrDefault(e => e.Name == segment);
                        element = prop.Value;
                    }

                    string replaceFrom = element.ValueKind == JsonValueKind.Number ? element.GetInt32().ToString(CultureInfo.InvariantCulture) : element.ToString();

                    ReadCodeSetting(propertyMapping.Represents, replaceFrom, projectAuthenticationSettings);

                    int index = GetIndex(element);
                    int length = replaceFrom.Length;

                    AddReplacement(projectAuthenticationSettings, filePath, index, length, replaceFrom, propertyMapping.Represents);
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
            Type type = element.GetType();
            object _idx = type.GetField("_idx",
                                        BindingFlags.NonPublic | BindingFlags.Instance).GetValue(element);
            return (int)_idx;
        }

        private static void AddReplacement(ProjectAuthenticationSettings projectAuthenticationSettings, string filePath, int index, int length, string replaceFrom, string replaceBy)
        {
            projectAuthenticationSettings.Replacements.Add(new Replacement(filePath, index, length, replaceFrom, replaceBy));
        }

    }
}
