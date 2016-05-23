using Microsoft.Extensions.Options;

namespace TestApp
{
    public class B2CAuthenticationOptions : IOptions<B2CAuthenticationOptions>
    {
        public string Instance { get; set; }
        public string TenantId { get; set; }

        public string Authority => $"{Instance}{TenantId}/v2.0";

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public string PostLogoutRedirectUri { get; set; }
        
        public B2CAuthenticationOptions Value => this;
    }
}