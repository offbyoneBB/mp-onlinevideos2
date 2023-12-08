using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebServiceCore.Models;
using WebServiceCore.Models.Entities;

namespace WebServiceCore.Pages.Sites
{
    public class SiteOverviewModel : PageModel
    {
        private readonly OnlineVideosDataContext _context;

        public SiteOverviewModel(OnlineVideosDataContext context)
        {
            _context = context;
        }

        public string CurrentSortingProperty { get; set; }

        public IList<Site> Sites { get; set; }

        public async Task OnGet()
        {
            await SetSites(null);
        }
                
        public async Task<IActionResult> OnGetSiteGrid(string sortingProperty)
        {
            await SetSites(sortingProperty);
            return Partial("_SiteGridPartial", this);
        }

        async Task SetSites(string sortingProperty)
        {
            IQueryable<Site> query = from s in _context.Sites select s;

            switch (sortingProperty)
            {
                case nameof(Site.OwnerId): query = query.OrderBy(s => s.OwnerId); break;
                case nameof(Site.Language): query = query.OrderBy(s => s.Language); break;
                case nameof(Site.LastUpdated): query = query.OrderBy(s => s.LastUpdated); break;
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
