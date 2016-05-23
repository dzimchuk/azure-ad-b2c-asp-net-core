using Microsoft.Extensions.Options;

namespace TestApp
{
    public class B2CPolicies : IOptions<B2CPolicies>
    {
        public string SignInPolicy { get; set; }
        public string SignUpPolicy { get; set; }
        public string EditProfilePolicy { get; set; }
        public B2CPolicies Value => this;
    }
}