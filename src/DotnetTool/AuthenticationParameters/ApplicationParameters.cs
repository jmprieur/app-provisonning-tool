using System.Collections.Generic;

namespace DotnetTool.AuthenticationParameters
{
    public class ApplicationParameters
    {
        /// <summary>
        /// Application display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Tenant in which the application is created
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Client ID of the application
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Sign-in audience (tenantId or domain, organizations, commong, consumers)
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Platforms
        /// </summary>
        public List<Platform> Platforms { get; } = new List<Platform>();

        /// <summary>
        /// API permissions
        /// </summary>
        public List<ApiPermission> ApiPermissions = new List<ApiPermission>();

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
    }
}
