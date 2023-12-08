using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;
using WebServiceCore.Models;
using WebServiceCore.Models.Entities;

namespace WebServiceCore.Services
{
    public class UserService : IUserService
    {
        private readonly OnlineVideosDataContext _context;

        public UserService(OnlineVideosDataContext context) 
        { 
            _context = context;
        }

        public async Task<ClaimsPrincipal> TryAuthenticate(string username, string password)
        {
            User user = await _context.Users.FindAsync(username);
            if (user == null || !CheckUserCredentials(user, password))
                return null;

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "user")
            };
            if (user.IsAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "admin"));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);            
        }

        bool CheckUserCredentials(User user, string password)
        {
            return user.Password == password;
        }
    }

    public static class OnlineVideosUserServiceExtensions
    {
        public static void AddOnlineVideosUserService(this IServiceCollection services) 
        {
            services.TryAddScoped<IUserService, UserService>();
        }
    }
}
