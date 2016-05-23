# azure-ad-b2c-asp-net-core

A sample demonstrating how you can configure your ASP.NET Core applications to take advantage of [Azure AD B2C](https://azure.microsoft.com/en-us/services/active-directory-b2c/) to perform such tasks as:
- Authenticate users
- Protect Web APIs
- Redeem authorization code
- Call a protected Web API

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
      "B2C": {
        "SignInPolicy": "e.g B2C_1_TestSignInPolicy",
        "SignUpPolicy": "e.g B2C_1_TestSignUpPolicy",
        "EditProfilePolicy": "e.g B2C_1_TestProfileEditPolicy"
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
      "SignInPolicy": "e.g B2C_1_TestSignInPolicy"
    }
  }
```