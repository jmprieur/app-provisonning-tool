// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DotnetTool.Project
{
    public class ConfigurationProperties
    {
        public static PropertyMapping[] s_emptyPropertyMappings = new PropertyMapping[0];

        public string FileRelativePath { get; set; }

        public PropertyMapping[] Properties { get; set; } = s_emptyPropertyMappings;


        public override string ToString()
        {
            return FileRelativePath;
        }
    }
}
