using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TestApp.Infrastructure;

namespace TestApp.Proxy
{
    public class TestServiceProxy
    {
        private readonly B2CAuthenticationOptions authOptions;
        private readonly B2CPolicies policies;
        private readonly TestServiceOptions serviceOptions;

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDistributedCache distributedCache;

        public TestServiceProxy(IOptions<B2CAuthenticationOptions> authOptions,
            IOptions<B2CPolicies> policies,
            IOptions<TestServiceOptions> serviceOptions, 
            IHttpContextAccessor httpContextAccessor, 
            IDistributedCache distributedCache)
        {
            this.authOptions = authOptions.Value;
            this.policies = policies.Value;
            this.serviceOptions = serviceOptions.Value;
            this.httpContextAccessor = httpContextAccessor;
            this.distributedCache = distributedCache;
        }

        public async Task<string> GetValuesAsync()
        {
            var client = new HttpClient { BaseAddress = new Uri(serviceOptions.BaseUrl, UriKind.Absolute) };
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

            return await client.GetStringAsync("api/values");
        }

        // this is how you get tokens obtained by the OIDC middleware
        //private Task<string> GetAccessTokenAsync()
        //{
        //    return httpContextAccessor.HttpContext.GetTokenAsync("access_token");
        //}

        // this is how you get tokens with MSAL
        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var principal = httpContextAccessor.HttpContext.User;

                var tokenCache = new DistributedTokenCache(distributedCache, principal.FindFirst(Constants.ObjectIdClaimType).Value).GetMSALCache();
                var client = new ConfidentialClientApplication(authOptions.ClientId,
                                                          authOptions.GetAuthority(principal.FindFirst(Constants.AcrClaimType).Value),
                                                          "https://app", // it's not really needed
                                                          new ClientCredential(authOptions.ClientSecret),
                                                          tokenCache,
                                                          null);

                var account = (await client.GetAccountsAsync()).FirstOrDefault();

                var result = await client.AcquireTokenSilentAsync(new[] { $"{authOptions.ApiIdentifier}/read_values" },
                    account);

                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                throw new ReauthenticationRequiredException();
            }
        }
    }
}