﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DotnetTool.Project
{
    public class PropertyMapping
    {
        /// <summary>
        /// Path to the property
        /// </summary>
        public string? Property { get; set; }

        /// <summary>
        /// Represented authentication property
        /// </summary>
        public string? Represents { get; set; }

        /// <summary>
        /// Which flag is set?
        /// </summary>
        public string? Sets { get; set; }

        public override string? ToString()
        {
            return Property;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Property) 
                && (!string.IsNullOrEmpty(Represents) || !string.IsNullOrEmpty(Sets));
        }
    }
}
