// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetTool.Project
{
    public class ProjectDescription
    {
        /// <summary>
        /// Empty files
        /// </summary>
        static ConfigurationProperties[] emptyFiles = new ConfigurationProperties[0];

        /// <summary>
        /// Identifier of the project description.
        /// For instance dotnet-webapi
        /// </summary>
        public string? Identifier { get; set; }

        public string? ProjectRelativeFolder { get; set; }

        public string? BasedOnProjectDescription { get; set; }

        public override string? ToString()
        {
            return Identifier;
        }

        /// <summary>
        /// Is the project description valid?
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            bool isValid = !string.IsNullOrEmpty(Identifier)
                && ProjectRelativeFolder != null
                && (ConfigurationProperties != null || MatchesForProjectType != null)
                && (ConfigurationProperties == null || !ConfigurationProperties.Any(c => !c.IsValid()))
                && (MatchesForProjectType == null || !MatchesForProjectType.Any(m => !m.IsValid()));

            return isValid;
        }

        public ConfigurationProperties[]? ConfigurationProperties { get; set; }
        
        public MatchesForProjectType[]? MatchesForProjectType { get;set; }

        public ProjectDescription GetBasedOnProject(IEnumerable<ProjectDescription> projects)
        {
            ProjectDescription baseProject = projects.FirstOrDefault(p => p.Identifier == BasedOnProjectDescription);
            if (!string.IsNullOrEmpty(BasedOnProjectDescription) && baseProject == null)
            {
                throw new FormatException($"In Project {ProjectRelativeFolder} BasedOn = {BasedOnProjectDescription} could not be found");
            }
            return baseProject;
        }

        /// <summary>
        /// Get all the files with including merged from BaseOn project recursively
        /// merging all the properties
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public IEnumerable<ConfigurationProperties> GetMergedFiles(IEnumerable<ProjectDescription> projects)
        {
            IEnumerable<ConfigurationProperties> files = GetBasedOnProject(projects)?.GetMergedFiles(projects) ?? emptyFiles;
            IEnumerable<ConfigurationProperties> allFiles = ConfigurationProperties != null ? files.Union(ConfigurationProperties) : files;
            var allFilesGrouped = allFiles.GroupBy(f => f.FileRelativePath);
            foreach (var fileGrouping in allFilesGrouped)
            {
                yield return new ConfigurationProperties
                {
                    FileRelativePath = fileGrouping.Key,
                    Properties = fileGrouping.SelectMany(f => f.Properties).ToArray(),
                };
            }
        }
    }
}
