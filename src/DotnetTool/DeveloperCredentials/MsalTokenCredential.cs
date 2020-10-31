using Azure.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetTool.DeveloperCredentials
{
    public class MsalTokenCredential : TokenCredential
    {
        public MsalTokenCredential(string tenantId, string username, string instance = "https://login.microsoftonline.com")
        {
            TenantId = tenantId;
            Instance = instance;
            Username = username;
        }

        private IPublicClientApplication App { get; set; }
        private string TenantId { get; set; }
        private string Instance { get; set; }
        private string Username { get; set; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task GetOrCreateApp()
        {
            if (App == null)
            {
                string cacheDir = Path.Combine(
                    Environment.GetEnvironmentVariable("USERPROFILE"),
                    @"AppData\Local\.IdentityService");
                var storageProperties =
                     new StorageCreationPropertiesBuilder("msal.cache", cacheDir, "1950a258-227b-4e31-a9cf-717495945fc2")
                     /*
                     .WithLinuxKeyring(
                         Config.LinuxKeyRingSchema,
                         Config.LinuxKeyRingCollection,
                         Config.LinuxKeyRingLabel,
                         Config.LinuxKeyRingAttr1,
                         Config.LinuxKeyRingAttr2)
                     .WithMacKeyChain(
                         Config.KeyChainServiceName,
                         Config.KeyChainAccountName)
                     */
                     .Build();

                App = PublicClientApplicationBuilder.Create(storageProperties.ClientId)
                  .WithRedirectUri("http://localhost")
                  .Build();

                // This hooks up the cross-platform cache into MSAL
                var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties).ConfigureAwait(false);
                cacheHelper.RegisterCache(App.UserTokenCache);

            }
        }

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            await GetOrCreateApp();
            AuthenticationResult result;
            var accounts = await App.GetAccountsAsync();
            IAccount account;

            if (!string.IsNullOrEmpty(Username))
            {
                account = accounts.FirstOrDefault(account => account.Username == Username);
            }
            else
            {
                account = accounts.FirstOrDefault();
            }
            try
            {
                result = await App.AcquireTokenSilent(requestContext.Scopes, account)
                    .WithAuthority(Instance, TenantId)
                    .ExecuteAsync(cancellationToken);
            }
            catch (MsalUiRequiredException ex)
            {
                result = await App.AcquireTokenInteractive(requestContext.Scopes)
                    .WithLoginHint(Username)
                    .WithClaims(ex.Claims)
                    .WithAuthority(Instance, TenantId)
                    .ExecuteAsync(cancellationToken);
            }
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }
    }
}
