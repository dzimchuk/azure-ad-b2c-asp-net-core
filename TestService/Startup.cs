using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TestService
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AuthenticationOptions>(configuration.GetSection("Authentication:AzureAd"));

            var provider = services.BuildServiceProvider();
            var authOptions = provider.GetService<IOptions<AuthenticationOptions>>();
            
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // sets both authenticate and challenge default schemes
                .AddJwtBearer(options =>
                {
                    options.MetadataAddress = $"{authOptions.Value.Authority}/.well-known/openid-configuration?p={authOptions.Value.SignInOrSignUpPolicy}";
                    options.Audience = authOptions.Value.Audience;
                });

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            IOptions<AuthenticationOptions> authOptions)
        {
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
