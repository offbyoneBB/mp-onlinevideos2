using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WebServiceCore.Models;
using WebServiceCore.Models.Entities;

namespace WebServiceCore.Pages.Reports
{
    public class NewReport
    {
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public ReportType Type { get; set; }
        [Required]
        public string Message { get; set; }
    }

    public class SiteReportsModel : PageModel
    {
        private readonly OnlineVideosDataContext _context;

        public SiteReportsModel(OnlineVideosDataContext context)
        {
            _context = context;
        }

        public IList<Report> Reports { get; set; }

        /// <summary>
        /// The name of the site. Populated automatically by the value contained in the url.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string Site { get; set; }

        public string SiteOwner { get; set; }

        public bool CanEdit { get; set; }

        /// <summary>
        /// Populated by the submit report form on the page.
        /// </summary>
        [BindProperty]
        public NewReport NewReport { get; set; }

        /// <summary>
        /// Called when the page is requested with a GET request.
        /// </summary>
        /// <returns></returns>
        public async Task OnGet()
        {
            await SetReports();
        }

        /// <summary>
        /// Called when a new report is POSTed.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostSubmitReport()
        {
            await TrySubmitReport();
            await SetReports();
            return Partial("_ReportGridPartial", this);
        }

        async Task<bool> TrySubmitReport()
        {
            var site = await _context.Sites.FindAsync(Site);
            if (site == null)
                return false;

            // Check that the user is either the owner of the site or an admin
            // ToDo: This appears to be the same check as used in the old site, although the old (and new) API allows any user
            // to submit a report so this appears to be inconsistent??
            if (!User.Identity.IsAuthenticated || (User.Identity.Name != site.OwnerId && !User.IsInRole("admin")))
                return false;

            // Are all required fields present?
            if (!ModelState.IsValid)
                return false;

            Report report = new Report()
            {
                SiteName = Site,
                Date = NewReport.Date,
                Type = NewReport.Type,
                Message = NewReport.Message
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Should be called on every page request, including after form submission, to ensure existing reports are always displayed.
        /// </summary>
        /// <returns></returns>
        async Task SetReports()
        {
            var site = await _context.Sites
                .AsNoTrackingWithIdentityResolution()
                .Include(s => s.Reports)
                .FirstOrDefaultAsync(s => s.Name == Site);

            if (site == null)
                return;

            SiteOwner = site.OwnerId;
            CanEdit = User.Identity.Name == site.OwnerId || User.IsInRole("admin");
            Reports = site.Reports;
        }
    }
}
