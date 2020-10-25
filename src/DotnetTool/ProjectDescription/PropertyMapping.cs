// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DotnetTool.Project
{
    public class PropertyMapping
    {
        public string Property { get; set; }

        public string Represents { get; set; }

        public override string ToString()
        {
            return Property;
        }
    }
}
