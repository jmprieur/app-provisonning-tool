using System;

namespace DotnetTool
{
    class Program
    {
        static void Main(string[] args)
        {
            // Read options
            ProvisioningToolOptions provisioningToolOptions = GetOptions(args);

            AppProvisionningTool appProvisionningTool = new AppProvisionningTool(provisioningToolOptions);
            appProvisionningTool.Run();
        }

        private static ProvisioningToolOptions GetOptions(string[] args)
        {
            return new ProvisioningToolOptions();
        }
    }
}
