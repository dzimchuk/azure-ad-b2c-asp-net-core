using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TestApp.Models;

namespace TestApp.Proxy
{
    public class TestServiceProxy
    {
        private readonly ITokenAcquisition tokenAcquisition;

        private readonly string baseUrl;
        private readonly string[] scopes;

        public TestServiceProxy(ITokenAcquisition tokenAcquisition, IConfiguration configuration)
        {
            this.tokenAcquisition = tokenAcquisition;

            baseUrl = configuration.GetValue<string>("TestService:BaseUrl");
            scopes = configuration.GetValue<string>("TestService:Scopes")?.Split(' ');
        }

        public async Task<IEnumerable<WeatherForecast>> GetWeatherForecastAsync()
        {
            var client = new HttpClient { BaseAddress = new Uri(baseUrl, UriKind.Absolute) };
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

            var response = await client.GetAsync("WeatherForecast");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();

                var forcast = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(content);
                return forcast;
            }

            return null;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                return await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
            }
            catch (MsalUiRequiredException)
            {
                throw new ReauthenticationRequiredException();
            }
        }
    }
}