using System.Collections.Generic;

namespace DotnetTool.AuthenticationParameters
{
    public class ApplicationParameters
    {
        /// <summary>
        /// Application display name
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Tenant in which the application is created
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Client ID of the application
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Sign-in audience (tenantId or domain, organizations, common, consumers)
        /// </summary>
        public string? SignInAudience { get; set; }

        /// <summary>
        /// Is authenticated with AAD
        /// </summary>
        public bool IsAAD { get; set; }

        /// <summary>
        /// Is authenticated with Azure AD B2C
        /// </summary>
        public bool IsB2C { get; set; }
        public bool HasAuthentication { get; set; }

        public bool IsWebApi { get; set; }

        public bool IsWebApp { get; set; }


        /// <summary>
        /// Platforms
        /// </summary>
        public List<Platform> Platforms { get; } = new List<Platform>();

        /// <summary>
        /// API permissions
        /// </summary>
        public List<ApiPermission> ApiPermissions { get; } = new List<ApiPermission>();

        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; set; }
    }
}
