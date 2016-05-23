using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace TestApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly B2CPolicies policies;

        public AccountController(IOptions<B2CPolicies> policies)
        {
            this.policies = policies.Value;
        }

        public IActionResult SignIn()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(
                    Constants.OpenIdConnectAuthenticationScheme,
                    new AuthenticationProperties(new Dictionary<string, string> { { Constants.B2CPolicy, policies.SignInPolicy } })
                    {
                        RedirectUri = "/"
                    });
            }

            return RedirectHome();
        }

        public IActionResult SignUp()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(
                    Constants.OpenIdConnectAuthenticationScheme,
                    new AuthenticationProperties(new Dictionary<string, string> { { Constants.B2CPolicy, policies.SignUpPolicy } })
                    {
                        RedirectUri = "/"
                    });
            }

            return RedirectHome();
        }

        public IActionResult Profile()
        {
            if (User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(
                    Constants.OpenIdConnectAuthenticationScheme,
                    new AuthenticationProperties(new Dictionary<string, string> { { Constants.B2CPolicy, policies.EditProfilePolicy } })
                    {
                        RedirectUri = "/"
                    });
            }

            return RedirectHome();
        }

        public async Task<IActionResult> SignOut()
        {
            if (User.Identity.IsAuthenticated)
            {
                await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                var callbackUrl = Url.Action("SignOutCallback", "Account", values: null, protocol: Request.Scheme);
                await HttpContext.Authentication.SignOutAsync(Constants.OpenIdConnectAuthenticationScheme,
                    new AuthenticationProperties(new Dictionary<string, string> { { Constants.B2CPolicy, User.FindFirst(Constants.AcrClaimType).Value } })
                    {
                        RedirectUri = callbackUrl
                    });

                return new EmptyResult();
            }

            return RedirectHome();
        }

        public IActionResult SignOutCallback()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                // Redirect to home page if the user is authenticated.
                return RedirectHome();
            }

            return View();
        }

        private IActionResult RedirectHome()
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}