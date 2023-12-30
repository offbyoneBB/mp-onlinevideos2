using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebServiceCore.Models;
using WebServiceCore.Models.Entities;
using WebServiceCore.Services;

namespace WebServiceCore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly OnlineVideosDataContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IUserService userService, OnlineVideosDataContext context, ILogger<IndexModel> logger)
        {
            _userService = userService;
            _context = context;
            _logger = logger;
        }

        public string CurrentSortingProperty { get; set; }

        public IList<Site> Sites { get; set; }

        public async Task OnGet()
        {
            // Populate the model with the list of sites using the default sorting
            await SetSites(null);
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

        /// <summary>
        /// Called using ajax to change the sorting of the site grid.
        /// </summary>
        /// <param name="sortingProperty">The property to use when sorting the sites.</param>
        /// <returns>_SiteGridPartial that contains just the table of sites.</returns>
        public async Task<IActionResult> OnGetSiteGrid(string sortingProperty)
        {
            // This model is newly created on every request so the sites always need be retrieved on every request.
            // ToDo: Make use of Razor TempData to persist the retrieved sites across requests
            await SetSites(sortingProperty);
            // Return the partial generated using this model.
            return Partial("_SiteGridPartial", this);
        }

        /// <summary>
        /// Gets the sites from the backend, sorts them, and sets <see cref="CurrentSortingProperty"/> and <see cref="Sites"/> with the results.
        /// </summary>
        /// <param name="sortingProperty">The property to use when sorting the sites.</param>
        /// <returns></returns>
        async Task SetSites(string sortingProperty)
        {
            IQueryable<Site> query = from s in _context.Sites select s;

            switch (sortingProperty)
            {
                case nameof(Site.OwnerId): query = query.OrderBy(s => s.OwnerId); break;
                case nameof(Site.Language): query = query.OrderBy(s => s.Language); break;
                case nameof(Site.LastUpdated): query = query.OrderBy(s => s.LastUpdated); break;
                // default to sorting by name
                default:
                    query = query.OrderBy(s => s.Name);
                    sortingProperty = nameof(Site.Name);
                    break;
            }
            CurrentSortingProperty = sortingProperty;

            Sites = await query
                .AsNoTrackingWithIdentityResolution()
                .Include(s => s.Reports)
                .ToListAsync();
        }
    }
}
