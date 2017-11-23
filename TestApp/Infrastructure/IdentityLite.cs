using Newtonsoft.Json;
using System.Collections.Generic;

namespace TestApp.Infrastructure
{
    internal class IdentityLite
    {
        public string AuthenticationType { get; set; }
        public string NameClaimType { get; set; }
        public string RoleClaimType { get; set; }
        
        public IEnumerable<ClaimLite> Claims { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static IdentityLite FromString(string value)
        {
            return JsonConvert.DeserializeObject<IdentityLite>(value);
        }
    }
}
