using WebServiceCore.Models.Entities;

namespace WebServiceCore.Models.Dto
{
    public class GetSiteDto
    {
        public string Name { get; set; }
        public SiteState State { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
        public bool IsAdult { get; set; }
        public string RequiredDll { get; set; }
        public string OwnerId { get; set; }
    }

    public static class GetSiteExtensions
    {
        public static GetSiteDto ToGetSiteDto(this Site site)
        {
            return new GetSiteDto
            {
                Name = site.Name,
                State = site.State,
                LastUpdated = site.LastUpdated,
                Language = site.Language,
                Description = site.Description,
                IsAdult = site.IsAdult,
                RequiredDll = site.DllId,
                OwnerId = site.OwnerId,
            };
        }
    }
}
