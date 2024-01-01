using System.Security.Claims;

namespace WebServiceCore.Services
{
    public interface IUserService
    {
        Task<ClaimsPrincipal> TryAuthenticate(string username, string password);
    }
}
