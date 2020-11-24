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

        public string? Domain { get; set; }

        public string? Authority { get; set; }

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

        /// <summary>
        /// The project has authentication
        /// </summary>
        public bool HasAuthentication { get; set; }

        /// <summary>
        /// The project is a web api
        /// </summary>
        public bool IsWebApi { get; set; }

        /// <summary>
        /// The project is a web app
        /// </summary>
        public bool IsWebApp { get; set; }

        /// <summary>
        /// The project is a blazor app
        /// </summary>
        public bool IsBlazor { get; set; }

        /// <summary>
        /// The app calls Microsoft Graph
        /// </summary>
        public bool CallsMicrosoftGraph { get; set; }

        /// <summary>
        /// The app calls a downstream API
        /// </summary>
        public bool CallsDownstreamApi { get; set; }

        /// <summary>
        /// Web app redirect URIs
        /// </summary>
        public List<string> WebRedirectUris { get; } = new List<string>();

        /// <summary>
        /// Callback path (path of the redirect URIs)
        /// </summary>
        public string? CallbackPath { set; get; }

        /// <summary>
        /// Logout URIs
        /// </summary>
        public string? LogoutUrl { set; get; }

        public List<string> PasswordCredentials { get; } = new List<string>();

        /// <summary>
        /// Identitier URIs for web APIs
        /// </summary>
        public List<string> IdentifierUris { get; } = new List<string>();

        /// <summary>
        /// API permissions
        /// </summary>
        public List<ApiPermission> ApiPermissions { get; } = new List<ApiPermission>();

        /// <summary>
        /// Description of the app
        /// </summary>
        public string? Description { get; set; }

        // SecretsId of the csproj
        public string SecretsId { get; internal set; }

        public string MsalAuthenticationOptions { get; set; }





        /// <summary>
        /// Sets a bool propery (from its name)
        /// </summary>
        /// <param name="propertyName"></param>
        public void Sets(string propertyName)
        {
            var property = GetType().GetProperty(propertyName);
            if (property == null)
            {
                throw new ArgumentException(propertyName);
            }
            property.SetValue(this, true);
        }
    }
}
