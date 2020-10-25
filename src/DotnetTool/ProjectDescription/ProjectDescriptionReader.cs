using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DotnetTool.Project
{
    public class ProjectDescriptionReader
    {
        public ProjectDescription GetProjectDescription(string projectTypeIdentifier, string codeFolder)
        {
            string projectTypeId = projectTypeIdentifier;
            if (string.IsNullOrEmpty(projectTypeId))
            {
                projectTypeId = InferProjectType(codeFolder);
            }

            return ReadProjectDescription(projectTypeId);
        }

        private ProjectDescription ReadProjectDescription(string projectTypeIdentifier)
        {
            string projectDescriptionFile = projectTypeIdentifier.Replace("-", "_");
            var properties = typeof(Properties.Resources).GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(p => p.PropertyType == typeof(byte[]));
            var property = properties.FirstOrDefault(p => p.Name == projectDescriptionFile);
            if (property != null)
            {
                return ReadDescriptionFromFileContent(property.GetValue(null) as byte[]);
            }
            return null;
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
            throw new NotImplementedException();
        }
    }

}