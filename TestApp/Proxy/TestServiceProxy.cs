using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TestApp.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Identity.Client;
using System.Linq;

namespace TestApp.Proxy
{
    public class TestServiceProxy
    {
        private readonly B2CAuthenticationOptions authOptions;
        private readonly TestServiceOptions serviceOptions;

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDistributedCache distributedCache;

        public TestServiceProxy(IOptions<B2CAuthenticationOptions> authOptions, IOptions<TestServiceOptions> serviceOptions, 
            IHttpContextAccessor httpContextAccessor, IDistributedCache distributedCache)
        {
            this.authOptions = authOptions.Value;
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
                var tokenCache = new DistributedTokenCache(distributedCache, httpContextAccessor.HttpContext.User.FindFirst(Constants.ObjectIdClaimType).Value).GetMSALCache();
                var client = new ConfidentialClientApplication(authOptions.ClientId,
                                                          authOptions.Authority,
                                                          "https://localhost:44397/",
                                                          new ClientCredential(authOptions.ClientSecret),
                                                          tokenCache,
                                                          null);

                var result = await client.AcquireTokenSilentAsync(new[] { $"{authOptions.ApiIdentifier}/read_values" },
                    client.Users.FirstOrDefault());

                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                throw new ReauthenticationRequiredException();
            }
        }
    }
}