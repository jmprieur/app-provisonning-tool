using DotnetTool.AuthenticationParameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotnetTool.CodeReaderWriter
{
    public class CodeWriter
    {
        internal void WriteConfiguration(Summary summary, List<Replacement> replacements, ApplicationParameters reconcialedApplicationParameters)
        {
            foreach (var replacementsInFile in replacements.GroupBy(r => r.FilePath))
            {
                string filePath = replacementsInFile.Key;

                string fileContent = File.ReadAllText(filePath);
                bool updated = false;
                foreach (Replacement r in replacementsInFile.OrderByDescending(r => r.Index))
                {
                    string replaceBy = ComputeReplacement(r.ReplaceBy, reconcialedApplicationParameters);
                    if (replaceBy != null && replaceBy!=r.ReplaceFrom)
                    {
                        int index = fileContent.IndexOf(r.ReplaceFrom /*, r.Index*/);
                        if (index != -1)
                        {
                            fileContent = fileContent.Substring(0, index)
                                + replaceBy
                                + fileContent.Substring(index + r.Length);
                            updated = true;
                            summary.changes.Add(new Change($"{filePath}: updating {r.ReplaceBy}"));
                        }
                    }
                }

                if (updated)
                {
                    // Keep a copy of the original
                    if (!File.Exists(filePath + "%"))
                    {
                        File.Copy(filePath, filePath + "%");
                    }
                    File.WriteAllText(filePath, fileContent);
                }
            }
        }

        private string ComputeReplacement(string replaceBy, ApplicationParameters reconcialedApplicationParameters)
        {
            string replacement = replaceBy;
            switch(replaceBy)
            {
                case "Application.ClientSecret":
                    string password = reconcialedApplicationParameters.PasswordCredentials.LastOrDefault();
                    if (!string.IsNullOrEmpty(reconcialedApplicationParameters.SecretsId))
                    {
                        // TODO: adapt for Linux: https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-5.0&tabs=windows#how-the-secret-manager-tool-works
                        string path = Path.Combine(
                            Environment.GetEnvironmentVariable("UserProfile"),
                            @"AppData\Roaming\Microsoft\UserSecrets\",
                            reconcialedApplicationParameters.SecretsId,
                            "secrets.json");
                        if (!File.Exists(path))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            string section = reconcialedApplicationParameters.IsB2C ? "AzureADB2C" : "AzureAD";
                            File.WriteAllText(path, $"{{\n    \"{section}:ClientSecret\": \"{password}\"\n}}");
                            replacement = "See user secrets";
                        }
                        else
                        {
                            replacement = password;
                        }
                    }
                    else
                    {
                        replacement = password;
                    }
                    break;
                case "Application.ClientId":
                    replacement = reconcialedApplicationParameters.ClientId;
                    break;
                case "Directory.TenantId":
                    replacement = reconcialedApplicationParameters.TenantId;
                    break;
                case "Directory.Domain":
                    replacement = reconcialedApplicationParameters.Domain;
                    break;
                case "Application.CallbackPath":
                    replacement = reconcialedApplicationParameters.CallbackPath;
                    break;
                case "profilesApplicationUrls":
                case "iisSslPort":
                case "iisApplicationUrl":
                    replacement = null;
                    break;
                case "secretsId":
                    replacement = reconcialedApplicationParameters.SecretsId;
                    break;
                case "Application.Authority":
                    replacement = reconcialedApplicationParameters.Authority;
                    break;
                case "MsalAuthenticationOptions":
                    // Todo generalize with a directive: Ensure line after line, or ensure line
                    // between line and line
                    replacement = reconcialedApplicationParameters.MsalAuthenticationOptions +
                        "\n                options.ProviderOptions.DefaultAccessTokenScopes.Add(\"User.Read\");";
                    break;

                case "Application.CalledApiScopes":
                    replacement = reconcialedApplicationParameters.CalledApiScopes
                        .Replace("openid", string.Empty)
                        .Replace("offline_access", string.Empty)
                        .Trim();
                    break;

                case "Application.Instance":
                    if (reconcialedApplicationParameters.Instance == "https://login.microsoftonline.com/tfp/"
                        && reconcialedApplicationParameters.IsB2C
                        && !string.IsNullOrEmpty(reconcialedApplicationParameters.Domain)
                        && reconcialedApplicationParameters.Domain.EndsWith(".onmicrosoft.com"))
                    {
                        replacement = "https://"+reconcialedApplicationParameters.Domain.Replace(".onmicrosoft.com", ".b2clogin.com");
                    }
                    else
                    {
                        replacement = reconcialedApplicationParameters.Instance;
                    }
                    break;
                default:
                    Console.WriteLine($"{replaceBy} not known");
                    break;
            }
            return replacement;
        }
    }
}
