namespace WebServiceCore.Models.Dto
{
    public class SubmitSiteDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string SiteXml { get; set; }
        public byte[] Icon { get; set; }
        public byte[] Banner { get; set; }
        public string RequiredDll { get; set; }
    }
}
