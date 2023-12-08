using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using WebServiceCore.Services;

namespace WebServiceCore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IUserService userService, ILogger<IndexModel> logger)
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
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLogout()
        {
            string username = HttpContext.User.Identity.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User {Email} logged out at {Time}", username, DateTime.UtcNow);
            return RedirectToPage();
        }
    }
}
