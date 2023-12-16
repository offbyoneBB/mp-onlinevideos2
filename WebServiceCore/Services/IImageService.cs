
namespace WebServiceCore.Services
{
    public interface IImageService
    {
        Task<string> TrySaveBanner(byte[] banner, string savePath);
        Task<string> TrySaveIcon(byte[] icon, string savePath);
    }
}