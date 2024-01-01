using WebServiceCore.Models.Entities;

namespace WebServiceCore.Models.Dto
{
    public class GetReportDto
    {
        public string SiteName { get; set; }
        public DateTime Date { get; set; }
        public ReportType Type { get; set; }
        public string Message { get; set; }
    }

    public static class GetReportExtensions
    {
        public static GetReportDto ToGetReportDto(this Report report)
        {
            return new GetReportDto
            {
                SiteName = report.SiteName,
                Date = report.Date,
                Type = report.Type,
                Message = report.Message
            };
        }
    }
}
