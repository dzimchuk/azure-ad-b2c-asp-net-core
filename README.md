# azure-ad-b2c-asp-net-core

A sample demonstrating how you can configure your ASP.NET Core 5.0 applications to take advantage of [Azure AD B2C](https://azure.microsoft.com/en-us/services/active-directory-b2c/) and [MSAL](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) to perform such tasks as:
- Authenticate users
- Protect Web APIs
- Redeem authorization code
- Call a protected Web API
- Implement self-service password reset
- Implement profile editing

Please find more information in this walk-through post:
[Setting up your ASP.NET Core 2.0 apps and services for Azure AD B2C](https://dzimchuk.net/setting-up-your-asp-net-core-2-0-apps-and-services-for-azure-ad-b2c/)

More documentation:

[Create user flows](https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-create-user-flows?pivots=b2c-user-flow)

# Configuration

## Web App

```
"Authentication": {
    "AzureAd": {
      "Instance": "e.g. https://login.microsoftonline.com/",
      "TenantId": "e.g. <your domain>.onmicrosoft.com>",
      "ClientId": "",
      "ClientSecret": "",
      "PostLogoutRedirectUri": "https://localhost:44397/",
      "ApiIdentifier": "https://<tenant name>.onmicrosoft.com/testapi",
      "B2C": {
        "SignInOrSignUpPolicy": "e.g B2C_1_TestSignUpAndSignInPolicy",
        "EditProfilePolicy": "e.g B2C_1_TestProfileEditPolicy",
        "ResetPasswordPolicy": "e.g. B2C_1_TestPasswordReset"
      }
    }
  },
  "TestServiceOptions": {
    "BaseUrl": "https://localhost:44359/"
  } 
```

## TestService

```
"Authentication": {
    "AzureAd": {
      "Instance": "e.g. https://login.microsoftonline.com/",
      "TenantId": "e.g. <your domain>.onmicrosoft.com>",
      "Audience": "Use client Id of the common app",
      "SignInOrSignUpPolicy": "e.g B2C_1_TestSignUpAndSignInPolicy"
    }
  }
```
