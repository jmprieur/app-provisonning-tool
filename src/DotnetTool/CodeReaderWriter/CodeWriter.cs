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
                    if (!File.Exists(filePath))
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
                    replacement = "Application.ClientSecret";
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
                default:
                    Console.WriteLine($"{replaceBy} not known");
                    break;
            }
            return replacement;
        }
    }
}
