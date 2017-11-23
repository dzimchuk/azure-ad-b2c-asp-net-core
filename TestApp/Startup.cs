using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TestApp.Infrastructure;
using TestApp.Proxy;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System;

namespace TestApp
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
            services.Configure<B2CAuthenticationOptions>(configuration.GetSection("Authentication:AzureAd"));
            services.Configure<B2CPolicies>(configuration.GetSection("Authentication:AzureAd:B2C"));

            services.Configure<TestServiceOptions>(configuration.GetSection("TestServiceOptions"));
            services.AddTransient<TestServiceProxy>();

            services.AddMvc(options => options.Filters.Add(typeof(ReauthenticationRequiredFilter)));

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDistributedMemoryCache();

            ConfigureAuthentication(services);
        }

        private static void ConfigureAuthentication(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();

            var authOptions = serviceProvider.GetService<IOptions<B2CAuthenticationOptions>>();
            var b2cPolicies = serviceProvider.GetService<IOptions<B2CPolicies>>();

            var distributedCache = serviceProvider.GetService<IDistributedCache>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Constants.OpenIdConnectAuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(Constants.OpenIdConnectAuthenticationScheme, options =>
            {
                options.Authority = authOptions.Value.Authority;
                options.ClientId = authOptions.Value.ClientId;
                options.ClientSecret = authOptions.Value.ClientSecret;
                options.SignedOutRedirectUri = authOptions.Value.PostLogoutRedirectUri;

                options.ConfigurationManager = new PolicyConfigurationManager(authOptions.Value.Authority,
                                               new[] { b2cPolicies.Value.SignInOrSignUpPolicy, b2cPolicies.Value.EditProfilePolicy });

                options.Events = CreateOpenIdConnectEventHandlers(authOptions.Value, b2cPolicies.Value, distributedCache);

                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name"
                };

                // it will fall back to DefaultSignInScheme if not set
                //options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // we don't want the middleware to redeem the authorization code
                //options.Scope.Add("offline_access");
                //options.Scope.Add($"{authOptions.Value.ApiIdentifier}/read_values");
                //options.SaveTokens = true;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            
            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private static OpenIdConnectEvents CreateOpenIdConnectEventHandlers(B2CAuthenticationOptions authOptions, B2CPolicies policies, IDistributedCache distributedCache)
        {
            return new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = async context => 
                                               {
                                                   await SetIssuerAddressAsync(context, policies.SignInOrSignUpPolicy);

                                                   if (IsProfileEditingRequest(context.Properties, policies))
                                                   {
                                                       var identity = (ClaimsIdentity)context.HttpContext.User.Identity;
                                                       var identityLite = new IdentityLite
                                                       {
                                                           AuthenticationType = identity.AuthenticationType,
                                                           NameClaimType = identity.NameClaimType,
                                                           RoleClaimType = identity.RoleClaimType,
                                                           Claims = context.HttpContext.User.Claims.Select(claim => new ClaimLite { Type = claim.Type, Value = claim.Value })
                                                       };
                                                       context.Properties.Items.Add(Constants.IdentityKey,  identityLite.ToString());
                                                   }
                                               },
                OnRedirectToIdentityProviderForSignOut = context => SetIssuerAddressForSignOutAsync(context, policies.SignInOrSignUpPolicy),
                OnAuthorizationCodeReceived = async context =>
                                              {
                                                  try
                                                  {
                                                      var userTokenCache = new DistributedTokenCache(distributedCache, context.Principal.FindFirst(Constants.ObjectIdClaimType).Value).GetMSALCache();
                                                      var client = new ConfidentialClientApplication(authOptions.ClientId,
                                                          authOptions.Authority,
                                                          context.TokenEndpointRequest.RedirectUri,
                                                          new ClientCredential(authOptions.ClientSecret),
                                                          userTokenCache,
                                                          null);

                                                      var result = await client.AcquireTokenByAuthorizationCodeAsync(context.TokenEndpointRequest.Code,
                                                          new[] { $"{authOptions.ApiIdentifier}/read_values" });

                                                      context.HandleCodeRedemption(result.AccessToken, result.IdToken);
                                                  }
                                                  catch(Exception e)
                                                  {
                                                      context.HandleResponse();
                                                  }
                                              },
                OnAuthenticationFailed = context =>
                {
                    context.Fail(context.Exception);
                    return Task.FromResult(0);
                },
                OnMessageReceived = context =>
                {
                    HandleProfileEditingCancellation(policies, context);

                    return Task.FromResult(0);
                }
            };
        }

        private static void HandleProfileEditingCancellation(B2CPolicies policies, MessageReceivedContext context)
        {
            if (!string.IsNullOrEmpty(context.ProtocolMessage.Error) &&
                                    !string.IsNullOrEmpty(context.ProtocolMessage.ErrorDescription) &&
                                    context.ProtocolMessage.ErrorDescription.StartsWith("AADB2C90091") &&
                                    IsProfileEditingRequest(context.Properties, policies))
            {
                var identityLite = IdentityLite.FromString(context.Properties.Items[Constants.IdentityKey]);
                var identity = new ClaimsIdentity(identityLite.Claims.Select(claim => new Claim(claim.Type, claim.Value)), 
                    identityLite.AuthenticationType, identityLite.NameClaimType, identityLite.RoleClaimType);

                context.Principal = new ClaimsPrincipal(identity);
                context.Success();
            }
        }

        private static bool IsProfileEditingRequest(AuthenticationProperties properties, B2CPolicies policies)
        {
            return properties.Items.ContainsKey(Constants.B2CPolicy) && properties.Items[Constants.B2CPolicy] == policies.EditProfilePolicy;
        }

        private static async Task SetIssuerAddressAsync(RedirectContext context, string defaultPolicy)
        {
            var configuration = await GetOpenIdConnectConfigurationAsync(context, defaultPolicy);
            context.ProtocolMessage.IssuerAddress = configuration.AuthorizationEndpoint;
        }

        private static async Task SetIssuerAddressForSignOutAsync(RedirectContext context, string defaultPolicy)
        {
            var configuration = await GetOpenIdConnectConfigurationAsync(context, defaultPolicy);
            context.ProtocolMessage.IssuerAddress = configuration.EndSessionEndpoint;
        }

        private static async Task<OpenIdConnectConfiguration> GetOpenIdConnectConfigurationAsync(RedirectContext context, string defaultPolicy)
        {
            var manager = (PolicyConfigurationManager)context.Options.ConfigurationManager;
            var policy = context.Properties.Items.ContainsKey(Constants.B2CPolicy) ? context.Properties.Items[Constants.B2CPolicy] : defaultPolicy;
            var configuration = await manager.GetConfigurationByPolicyAsync(CancellationToken.None, policy);
            return configuration;
        }
    }
}
