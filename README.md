# azure-ad-b2c-asp-net-core

A sample demonstrating how you can configure your ASP.NET Core 6.0 applications to take advantage of [Azure AD B2C](https://azure.microsoft.com/en-us/services/active-directory-b2c/), [Microsoft Identity Web](https://github.com/AzureAD/microsoft-identity-web) and [MSAL](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) to perform such tasks as:
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
    "AzureAdB2C": {
      "Instance": "https://{your-tenant-name}.b2clogin.com",
      "Domain": "<your-tenant-name>.onmicrosoft.com",
      "ClientId": "<client id>",
      "CallbackPath": "/signin-oidc",
      "SignedOutCallbackPath": "/signout/B2C_1_SignUpAndSignIn",
      "SignUpSignInPolicyId": "B2C_1_SignUpAndSignIn",
      "ResetPasswordPolicyId": "B2C_1_PasswordReset",
      "EditProfilePolicyId": "B2C_1_ProfileEdit",
      // To call an API
      "ClientSecret": "[client secret]"
    }
  },
  "TestService": {
    "BaseUrl": "https://localhost:5001",
    "Scopes": "e.g. https://{your-tenant-name}.onmicrosoft.com/testapi/read_values"
  }
```

## TestService

```
"Authentication": {
    "AzureAdB2C": {
      "Instance": "https://{your-tenant-name}.b2clogin.com",
      "Domain": "<your-tenant-name>.onmicrosoft.com",
      "ClientId": "<client id>",
      "SignUpSignInPolicyId": "B2C_1_SignUpAndSignIn"
    }
  }
```
