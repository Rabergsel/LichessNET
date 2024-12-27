using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using LichessNET.Entities.Enumerations;
using LichessNET.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

public static class LichessOAuthSetup
{
    public static void AddOAuthAuthentication(this IServiceCollection services, IConfiguration configuration,
        params LichessScope[] scopes)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "Lichess";
            })
            .AddCookie()
            .AddOAuth("Lichess", options =>
            {
                options.ClientId = configuration["Lichess:ClientId"];
                options.ClientSecret = configuration["Lichess:ClientSecret"];
                options.CallbackPath = new PathString("/signin-lichess");

                options.AuthorizationEndpoint = "https://lichess.org/oauth";
                options.TokenEndpoint = "https://lichess.org/api/token";
                options.UserInformationEndpoint = "https://lichess.org/api/account";

                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");

                options.UsePkce = true; // Enable PKCE

                // Add scopes
                foreach (var scope in scopes)
                {
                    options.Scope.Add(scope.GetEnumMemberValue());
                }

                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                        var response = await context.Backchannel.SendAsync(request,
                            HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                        context.RunClaimActions(user.RootElement);
                    }
                };
            });
    }

    public static void UseOAuthAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
    }
}