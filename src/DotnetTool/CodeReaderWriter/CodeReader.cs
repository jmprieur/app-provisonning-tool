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
            var properties = projectDescription.GetMergedConfigurationProperties(projectDescriptions).ToArray();
            foreach (ConfigurationProperties configurationProperties in properties)
            {
                string filePath = Path.Combine(projectPath, configurationProperties.FileRelativePath!).Replace('/', '\\');
                ProcessFile(projectAuthenticationSettings, filePath, configurationProperties);
            }


            foreach(var matchesForProjectType in projectDescription.GetMergedMatchesForProjectType(projectDescriptions))
            {
                if (!string.IsNullOrEmpty(matchesForProjectType.Sets))
                {
                    projectAuthenticationSettings.ApplicationParameters.Sets(matchesForProjectType.Sets);
                }
            }

            PostProcessWebUris(projectAuthenticationSettings);
        }

        private static void PostProcessWebUris(ProjectAuthenticationSettings projectAuthenticationSettings)
        {
            string callbackPath = projectAuthenticationSettings.ApplicationParameters.CallbackPath ?? "/signin-oidc";
            if (callbackPath != null && !callbackPath.StartsWith('/'))
            {
                projectAuthenticationSettings.ApplicationParameters.WebRedirectUris.Add(callbackPath);
            }
            else
            {
                List<string> launchUrls = new List<string>();

                // Create a list of string (roots)
                string? iisExpressApplicationUrl = projectAuthenticationSettings.Replacements.FirstOrDefault(r => r.ReplaceBy == "iisApplicationUrl")?.ReplaceFrom;
                string? iisExpressSslPort = projectAuthenticationSettings.Replacements.FirstOrDefault(r => r.ReplaceBy == "iisSslPort")?.ReplaceFrom;
                if (!string.IsNullOrEmpty(iisExpressApplicationUrl) && !string.IsNullOrEmpty(iisExpressSslPort))
                {
                    // Change the port
                    string sslLauchUrl = iisExpressApplicationUrl.Replace("http://", "https://")
                        .Substring(0, iisExpressApplicationUrl.LastIndexOf(":")) + ":" + iisExpressSslPort;
                    launchUrls.Add(sslLauchUrl);
                }

                // Add the profile lauchsettings 
                IEnumerable<string> httpsProfileLaunchUrls = projectAuthenticationSettings.Replacements
                    .Where(r => r.ReplaceBy == "profilesApplicationUrls")
                    .SelectMany(r => r.ReplaceFrom.Split(';'))
                    .Where(u => u.StartsWith("https://"));
                launchUrls.AddRange(httpsProfileLaunchUrls);

                // Set the Web redirect URIS
                List<string> redirectUris = projectAuthenticationSettings.ApplicationParameters.WebRedirectUris;
                redirectUris.AddRange(launchUrls.Select(l => l + callbackPath));

                // Get the signout path (/oidc-signout)
                string signoutPath = "/oidc-signout";
                if (!string.IsNullOrEmpty(signoutPath))
                {
                    if (signoutPath.StartsWith("/"))
                    {
                        if (launchUrls.Any())
                        {
                            projectAuthenticationSettings.ApplicationParameters.LogoutUrl = launchUrls.First() + signoutPath;
                        }
                    }
                    else
                    {
                        projectAuthenticationSettings.ApplicationParameters.LogoutUrl = signoutPath;
                    }
                }
            }
        }

        private static void ProcessFile(ProjectAuthenticationSettings projectAuthenticationSettings, string filePath, ConfigurationProperties file)
        {
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

                        IEnumerable<KeyValuePair<JsonElement, int>> elements = FindMatchingElements(jsonContent, path, 0);
                        foreach (var pair in elements)
                        {
                            JsonElement element = pair.Key;
                            int index = pair.Value;
                            found = true;
                            string replaceFrom = element.ValueKind == JsonValueKind.Number ? element.GetInt32().ToString(CultureInfo.InvariantCulture) : element.ToString();

                            if (!string.IsNullOrEmpty(propertyMapping.Represents))
                            {
                                ReadCodeSetting(propertyMapping.Represents, replaceFrom, propertyMapping.Default, projectAuthenticationSettings);
                                int length = replaceFrom.Length;

                                AddReplacement(projectAuthenticationSettings, filePath, index, length, replaceFrom, propertyMapping.Represents);
                            }


                        }
                    }

                    if (!string.IsNullOrEmpty(propertyMapping.Sets) && (found
                            || (propertyMapping.MatchAny != null && propertyMapping.MatchAny.Any(m => fileContent.Contains(m)))))
                    {
                        projectAuthenticationSettings.ApplicationParameters.Sets(propertyMapping.Sets);
                    }
                }
                // TODO: else AddNotFound?
            }
        }

        /// <summary>
        /// Recursively finds the JSon elements matching the path
        /// </summary>
        /// <param name="parentElement">parent JsonElement</param>
        /// <param name="path">JSon path to match. Note that "*" can be used to match anything
        /// (for example path representing "profiles:*:applicationUrl" in an appsettings.json to 
        /// get all the redirect URIs)</param>
        /// <returns>An enumeration of JSonElement matching the path</returns>
        private static IEnumerable<KeyValuePair<JsonElement, int>> FindMatchingElements(JsonElement parentElement, IEnumerable<string> path, int offset)
        {
            if (path.Any())
            {
                string segment = path.First();

                IEnumerable<JsonProperty> props = parentElement.EnumerateObject()
                    .Where(e => segment == "*" || e.Name == segment);
                foreach (JsonProperty prop in props)
                {
                    if (prop.Value.ValueKind != JsonValueKind.Undefined)
                    {
                        JsonElement element = prop.Value;
                        int index = GetIndex(element) + offset;

                        if (path.Count() == 1)
                        {
                            yield return new KeyValuePair<JsonElement, int>(element, index);
                        }
                        foreach (KeyValuePair<JsonElement, int> child in FindMatchingElements(element, path.Skip(1), index))
                        {
                            yield return child;
                        }
                    }
                }
            }
        }

        private static void ReadCodeSetting(string represents, string value, string? defaultValue, ProjectAuthenticationSettings projectAuthenticationSettings)
        {
            if (value != defaultValue)
            {
                switch (represents)
                {
                    case "Application.ClientId":
                        projectAuthenticationSettings.ApplicationParameters.ClientId = value;
                        break;
                    case "Application.CallbackPath":
                        projectAuthenticationSettings.ApplicationParameters.CallbackPath = value ?? defaultValue;
                        break;
                    case "Directory.TenantId":
                        projectAuthenticationSettings.ApplicationParameters.TenantId = value;
                        break;
                    case "Directory.Domain":
                        projectAuthenticationSettings.ApplicationParameters.Domain = value;
                        break;
                }
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
