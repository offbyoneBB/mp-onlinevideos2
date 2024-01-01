using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace WebServiceCore.Services
{
    public class ImageService : IImageService
    {
        public async Task<string> TrySaveIcon(byte[] icon, string savePath)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(icon))
                {
                    var imageInfo = await Image.IdentifyAsync(ms);
                    if (!PngFormat.Instance.Equals(imageInfo.Metadata.DecodedImageFormat))
                        return "Icon not saved! Must be PNG!";
                    if (imageInfo.Width != imageInfo.Height)
                        return "Icon not saved! Height must be equal to width!";
                    await File.WriteAllBytesAsync(savePath, icon);
                    return "Icon saved!";
                }
            }
            catch
            {
                return "Icon invalid!";
            }
        }

        public async Task<string> TrySaveBanner(byte[] banner, string savePath)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(banner))
                {
                    var imageInfo = await Image.IdentifyAsync(ms);
                    if (!PngFormat.Instance.Equals(imageInfo.Metadata.DecodedImageFormat))
                        return "Banner not saved! Must be PNG!";
                    if (imageInfo.Width != 3 * imageInfo.Height)
                        return "Banner not saved! Width must be 3 times height!";
                    await File.WriteAllBytesAsync(savePath, banner);
                    return "Banner saved!";
                }
            }
            catch
            {
                return "Banner invalid!";
            }
        }
    }
}
