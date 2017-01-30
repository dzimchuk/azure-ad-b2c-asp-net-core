using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Experimental.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TestApp.Infrastructure;
using TestApp.Proxy;

namespace TestApp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets();
            }

            Configuration = builder.Build();
        }

        private IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<B2CAuthenticationOptions>(Configuration.GetSection("Authentication:AzureAd"));
            services.Configure<B2CPolicies>(Configuration.GetSection("Authentication:AzureAd:B2C"));

            services.Configure<TestServiceOptions>(Configuration.GetSection("TestServiceOptions"));
            services.AddTransient<TestServiceProxy>();

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            IOptions<B2CAuthenticationOptions> authOptions, IOptions<B2CPolicies> b2cPolicies)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

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

            app.UseCookieAuthentication(new CookieAuthenticationOptions
                                        {
                                            AutomaticAuthenticate = true
                                        });

            var openIdConnectOptions = new OpenIdConnectOptions
                                       {
                                           AuthenticationScheme = Constants.OpenIdConnectAuthenticationScheme,
                                           AutomaticChallenge = true,

                                           Authority = authOptions.Value.Authority,
                                           ClientId = authOptions.Value.ClientId,
                                           ClientSecret = authOptions.Value.ClientSecret,
                                           PostLogoutRedirectUri = authOptions.Value.PostLogoutRedirectUri,

                                           ConfigurationManager = new PolicyConfigurationManager(authOptions.Value.Authority, 
                                               new[] { b2cPolicies.Value.SignInOrSignUpPolicy, b2cPolicies.Value.EditProfilePolicy }),
                                           Events = CreateOpenIdConnectEventHandlers(authOptions.Value, b2cPolicies.Value),

                                           ResponseType = OpenIdConnectResponseType.CodeIdToken,
                                           TokenValidationParameters = new TokenValidationParameters
                                                                       {
                                                                           NameClaimType = "name"
                                                                       },

                                           SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme
                                       };

            openIdConnectOptions.Scope.Add("offline_access");

            app.UseOpenIdConnectAuthentication(openIdConnectOptions);

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private static IOpenIdConnectEvents CreateOpenIdConnectEventHandlers(B2CAuthenticationOptions authOptions, B2CPolicies policies)
        {
            return new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = context => SetIssuerAddressAsync(context, policies.SignInOrSignUpPolicy),
                OnRedirectToIdentityProviderForSignOut = context => SetIssuerAddressForSignOutAsync(context, policies.SignInOrSignUpPolicy),
                OnAuthorizationCodeReceived = async context =>
                                              {
                                                  var credential = new ClientCredential(authOptions.ClientId, authOptions.ClientSecret);
                                                  var authenticationContext = new AuthenticationContext(authOptions.Authority);
                                                  var result = await authenticationContext.AcquireTokenByAuthorizationCodeAsync(context.TokenEndpointRequest.Code,
                                                      new Uri(context.TokenEndpointRequest.RedirectUri, UriKind.RelativeOrAbsolute), credential,
                                                      new[] { authOptions.ClientId }, context.Ticket.Principal.FindFirst(Constants.AcrClaimType).Value);

                                                  context.HandleCodeRedemption();
                                              },
                OnAuthenticationFailed = context =>
                {
                    context.HandleResponse();
                    context.Response.Redirect("/home/error");
                    return Task.FromResult(0);
                },
                OnMessageReceived = context =>
                {
                    if (!string.IsNullOrEmpty(context.ProtocolMessage.Error) &&
                        !string.IsNullOrEmpty(context.ProtocolMessage.ErrorDescription) &&
                        context.ProtocolMessage.ErrorDescription.StartsWith("AADB2C90091") &&
                        context.Properties.Items[Constants.B2CPolicy] == policies.EditProfilePolicy)
                    {
                        context.Ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(context.HttpContext.User, context.Properties, Constants.OpenIdConnectAuthenticationScheme);
                        context.HandleResponse();
                    }

                    return Task.FromResult(0);
                }
            };
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
