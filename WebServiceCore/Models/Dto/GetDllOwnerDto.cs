using WebServiceCore.Models.Entities;

namespace WebServiceCore.Models.Dto
{
    public class GetDllOwnerDto
    {
        public string OwnerId { get; set; }
        public string MD5 { get; set; }
    }

    public static class GetDllOwnerExtensions
    {
        public static GetDllOwnerDto ToGetDllOwnerDto(this Dll dll)
        {
            return new GetDllOwnerDto
            {
                OwnerId = dll.OwnerId,
                MD5 = dll.MD5
            };
        }
    }
}
