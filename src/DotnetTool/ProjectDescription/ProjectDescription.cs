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
        static File[] emptyFiles = new File[0];

        /// <summary>
        /// Identifier of the project description.
        /// For instance dotnet-webapi
        /// </summary>
        public string Identifier { get; set; }


        public string ProjectRelativeFolder { get; set; }

        public string BasedOnProjectDescription { get; set; }

        public override string ToString()
        {
            return Identifier;
        }

        public File[] Files { get; set; }

        public ProjectDescription GetBasedOnProject(IEnumerable<ProjectDescription> projects)
        {
            ProjectDescription baseProject = projects.FirstOrDefault(p => p.ProjectRelativeFolder == BasedOnProjectDescription);
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
        public IEnumerable<File> GetMergedFiles(IEnumerable<ProjectDescription> projects)
        {
            IEnumerable<File> files = GetBasedOnProject(projects)?.GetMergedFiles(projects) ?? emptyFiles;
            IEnumerable<File> allFiles = Files != null ? files.Union(Files) : files;
            var allFilesGrouped = allFiles.GroupBy(f => f.FileRelativePath);
            foreach (var fileGrouping in allFilesGrouped)
            {
                yield return new File
                {
                    FileRelativePath = fileGrouping.Key,
                    Properties = fileGrouping.SelectMany(f => f.Properties).ToArray(),
                };
            }
        }
    }
}
