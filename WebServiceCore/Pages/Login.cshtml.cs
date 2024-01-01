using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using WebServiceCore.Services;

namespace WebServiceCore.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IUserService userService, ILogger<LoginModel> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostLogin(string username, string password)
        {
            ClaimsPrincipal principal = await _userService.TryAuthenticate(username, password);
            if (principal == null)
                return Page();
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            _logger.LogInformation("User {Email} logged in at {Time}", username, DateTime.UtcNow);
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostLogout()
        {
            string username = HttpContext.User.Identity.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User {Email} logged out at {Time}", username, DateTime.UtcNow);
            return RedirectToPage("/Index");
        }
    }
}
