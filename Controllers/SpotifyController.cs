using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;

namespace dotnet_core_spotify_authentication.Controllers
{
    public class SpotifyAuthentication
    {
        public string clientID     = Environment.GetEnvironmentVariable("SpotifyClientID",EnvironmentVariableTarget.User);
        public string clientSecret = Environment.GetEnvironmentVariable("SpotifyClientSecret",EnvironmentVariableTarget.User);
        public string redirectURL  = "http://localhost:5000/callback";
    }

    class SpotifyResponse
    {
        public string access_token = "";
        public string token_type = "";
        public string expires_in = "";
        public string refresh_token = "";
        public string scope = "";
    }

    [Route("")]
    public class SpotifyController : Controller
    {
        SpotifyAuthentication sAuth = new SpotifyAuthentication();

        [HttpGet]
        public IActionResult Index()
        {   
            ViewData["response_type"] = "code";
            ViewData["client_id"] = sAuth.clientID;
            ViewData["client_secret"] = sAuth.clientSecret;
            ViewData["client_scope"] = "user-read-private user-read-email user-modify-playback-state user-read-playback-state";
            ViewData["redirect_uri"] = sAuth.redirectURL;

            return View();
        }

        [HttpPost("/client-store")]
        public ContentResult ClientStore([FromBody] SpotifyAuthentication sAuthStore)
        {
            this.sAuth = sAuthStore;
            var basicBearer = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(sAuthStore.clientID+":"+sAuthStore.clientSecret));

            Environment.SetEnvironmentVariable("SpotifyClientID",sAuthStore.clientID,EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("SpotifyClientSecret",sAuthStore.clientSecret,EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("SpotifyBasicBearer",basicBearer,EnvironmentVariableTarget.User);

            string responseString = "Your basic bearer: " + basicBearer;

            return new ContentResult {
                ContentType = "application/json",
                Content = responseString
            };
        }

        [Route("/callback")]
        public ContentResult Get(string code)
        {
            string responseString = "";

            if (code.Length > 0)
            {
                using (HttpClient client = new HttpClient())
                {
                    Console.WriteLine(Environment.NewLine+"Your basic bearer: "+Environment.GetEnvironmentVariable("SpotifyBasicBearer",EnvironmentVariableTarget.User));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Environment.GetEnvironmentVariable("SpotifyBasicBearer",EnvironmentVariableTarget.User));

                    FormUrlEncodedContent formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("redirect_uri", sAuth.redirectURL),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    });

                    var response = client.PostAsync("https://accounts.spotify.com/api/token", formContent).Result;

                    var responseContent = response.Content;
                    responseString = responseContent.ReadAsStringAsync().Result;
                    
                    SpotifyResponse sAuthResponse = JsonConvert.DeserializeObject<SpotifyResponse>(responseString);

                    Environment.SetEnvironmentVariable("SpotifyAccessToken",sAuthResponse.access_token,EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable("SpotifyRefreshToken",sAuthResponse.refresh_token,EnvironmentVariableTarget.User);
                }
            }

            return new ContentResult {
                ContentType = "application/json",
                Content = responseString
            };
        }
    }
}
