using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Identity.Client;
using System;

namespace TestApp.Infrastructure
{
    internal class DistributedTokenCache
    {
        private readonly IDistributedCache distributedCache;
        private readonly string userId;

        private readonly TokenCache tokenCache = new TokenCache();

        public DistributedTokenCache(IDistributedCache cache, string userId)
        {
            this.distributedCache = cache;
            this.userId = userId;

            tokenCache.SetBeforeAccess(OnBeforeAccess);
            tokenCache.SetAfterAccess(OnAfterAccess);
        }

        public TokenCache GetMSALCache() => tokenCache;

        private void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            var userTokenCachePayload = distributedCache.Get(CacheKey);
            if (userTokenCachePayload != null)
            {
                tokenCache.Deserialize(userTokenCachePayload);
            }
        }

        private void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14)
                };

                distributedCache.Set(CacheKey, tokenCache.Serialize(), cacheOptions);
            }
        }

        private string CacheKey => $"TokenCache_{userId}";

    }
}
