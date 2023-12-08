using WebServiceCore.Models.Entities;

namespace WebServiceCore.Models.Dto
{
    public class SubmitReportDto
    {
        public string SiteName { get; set; }
        public string Message { get; set; }
        public ReportType Type { get; set; }
    }
}
