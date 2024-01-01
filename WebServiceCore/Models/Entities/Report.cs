using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceCore.Models.Entities
{
    public enum ReportType : byte { Suggestion, Broken, ConfirmedBroken, RejectedBroken, Fixed };

    public class Report
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Site))]
        public string SiteName { get; set; }

        public DateTime Date { get; set; }

        public string Message { get; set; }

        public ReportType Type { get; set; }

        public Site Site { get; set; }
    }
}
