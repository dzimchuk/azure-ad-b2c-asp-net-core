using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            var authOptions = configuration.GetSection("Authentication:AzureAd").Get<AuthenticationOptions>();
            
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // sets both authenticate and challenge default schemes
                .AddJwtBearer(options =>
                {
                    options.MetadataAddress = $"{authOptions.Authority}/.well-known/openid-configuration?p={authOptions.SignInOrSignUpPolicy}";
                    options.Audience = authOptions.Audience;
                });

            services.AddControllers();

            services.AddAuthorization(options =>
                options.AddPolicy("ReadValuesPolicy", config => config.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", new[] { "read_values" })));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
