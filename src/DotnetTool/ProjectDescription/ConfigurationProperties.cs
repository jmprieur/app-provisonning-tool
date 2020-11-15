// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

namespace DotnetTool.Project
{
    public class ConfigurationProperties
    {
        public static PropertyMapping[] s_emptyPropertyMappings = new PropertyMapping[0];

        public string? FileRelativePath { get; set; }

        public PropertyMapping[] Properties { get; set; } = s_emptyPropertyMappings;


        public override string? ToString()
        {
            return FileRelativePath;
        }

        public bool IsValid()
        {
            bool valid = !string.IsNullOrEmpty(FileRelativePath) 
                && !Properties.Any(p => !p.IsValid());
            return valid;
        }
    }
}
