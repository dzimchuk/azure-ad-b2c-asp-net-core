using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;

namespace TestApp.Infrastructure
{
    internal class ReauthenticationRequiredFilter : IExceptionFilter
    {
        private readonly IOptionsMonitor<MicrosoftIdentityOptions> optionsMonitor;

        public ReauthenticationRequiredFilter(IOptionsMonitor<MicrosoftIdentityOptions> optionsMonitor)
        {
            this.optionsMonitor = optionsMonitor;
        }

        public void OnException(ExceptionContext context)
        {
            if (!context.ExceptionHandled && IsReauthenticationRequired(context.Exception))
            {
                const string scheme = OpenIdConnectDefaults.AuthenticationScheme;
                context.Result = new ChallengeResult(
                        scheme,
                        new AuthenticationProperties(new Dictionary<string, string> { { Constants.Policy, optionsMonitor.Get(scheme).SignUpSignInPolicyId } })
                        {
                            RedirectUri = context.HttpContext.Request.Path
                        });

                context.ExceptionHandled = true;
            }
        }

        private static bool IsReauthenticationRequired(Exception exception)
        {
            if (exception is ReauthenticationRequiredException)
            {
                return true;
            }

            if (exception.InnerException != null)
            {
                return IsReauthenticationRequired(exception.InnerException);
            }

            return false;
        }
    }
}
