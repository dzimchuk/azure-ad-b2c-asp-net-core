using Microsoft.Extensions.Options;

namespace TestApp.Proxy
{
    public class TestServiceOptions : IOptions<TestServiceOptions>
    {
        public string BaseUrl { get; set; }
        public TestServiceOptions Value => this;
    }
}