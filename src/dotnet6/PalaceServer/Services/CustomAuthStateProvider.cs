using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace PalaceServer.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        public CustomAuthStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            this.HttpContextAccessor = httpContextAccessor;
        }

        protected IHttpContextAccessor HttpContextAccessor { get; set; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = HttpContextAccessor.HttpContext.User;
            return Task.FromResult(new AuthenticationState(user));
        }
    }
}
