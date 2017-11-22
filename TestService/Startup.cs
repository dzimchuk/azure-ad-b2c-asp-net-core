using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            var serviceProvider = services.BuildServiceProvider();
            var authOptions = serviceProvider.GetService<IOptions<AuthenticationOptions>>();
            
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // sets both authenticate and challenge default schemes
                .AddJwtBearer(options =>
                {
                    options.MetadataAddress = $"{authOptions.Value.Authority}/.well-known/openid-configuration?p={authOptions.Value.SignInOrSignUpPolicy}";
                    options.Audience = authOptions.Value.Audience;
                });

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
