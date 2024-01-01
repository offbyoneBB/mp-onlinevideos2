using WebServiceCore.Models.Entities;

namespace WebServiceCore.Models.Dto
{
    public class GetDllDto
    {
        public string Name { get; set; }
        public DateTime LastUpdated { get; set; }
        public string OwnerId { get; set; }
        public string MD5 { get; set; }
    }

    public static class GetDllExtensions
    {
        public static GetDllDto ToGetDllDto(this Dll dll)
        {
            return new GetDllDto
            {
                Name = dll.Name,
                LastUpdated = dll.LastUpdated,
                OwnerId = dll.OwnerId,
                MD5 = dll.MD5
            };
        }
    }
}
