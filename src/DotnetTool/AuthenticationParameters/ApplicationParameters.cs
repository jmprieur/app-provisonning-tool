using System;
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
        /// Is authenticated with Azure AD B2C (set by reflection)
        /// </summary>
        public bool IsB2C { get; set; }
        public bool HasAuthentication { get; set; }

        public bool IsWebApi { get; set; }

        public bool IsWebApp { get; set; }

        public bool CallsMicrosoftGraph { get; set; }

        public bool CallsDownstreamApi { get; set; }

        // ImplicityGrantSettings.EnableIdTokenIssuance

        public List<string> RedirectUris { get; } = new List<string>();

        public string? LogoutUrl { set; get; }

        public List<string> PasswordCredentials { get; } = new List<string>();

        public List<string> IdentifierUris { get; } = new List<string>();
        /// <summary>
        /// API permissions
        /// </summary>
        public List<ApiPermission> ApiPermissions { get; } = new List<ApiPermission>();

        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; set; }

        public void Sets(string propertyName)
        {
            var property = GetType().GetProperty(propertyName);
            if (property == null)
            {
                throw new ArgumentException(propertyName);
            }
            Console.WriteLine($"setting {propertyName}");
            property.SetValue(this, true);
        }
    }
}
