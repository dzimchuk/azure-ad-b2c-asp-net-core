using Microsoft.Extensions.Options;

namespace TestService
{
    public class AuthenticationOptions : IOptions<AuthenticationOptions>
    {
        public string Instance { get; set; }
        public string TenantId { get; set; }

        public string Authority => $"{Instance}{TenantId}/v2.0";

        public string Audience { get; set; }
        public string SignInPolicy { get; set; }

        public AuthenticationOptions Value => this;
    }
}