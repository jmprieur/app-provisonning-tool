using DotnetTool.AuthenticationParameters;
using System.Collections.Generic;

namespace DotnetTool.CodeReaderWriter
{
    public class ProjectAuthenticationSettings
    {
        public ApplicationParameters ApplicationParameters { get; } = new ApplicationParameters();

        public List<Replacement> Replacements { get; } = new List<Replacement>();
    }
}
