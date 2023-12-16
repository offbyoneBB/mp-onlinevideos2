using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WebServiceCore.Services
{
    public static class ServiceExtensions
    {
        public static void AddOnlineVideosServices(this IServiceCollection services)
        {
            services.AddOnlineVideosUserService();
            services.AddOnlineVideosImageService();
        }

        public static void AddOnlineVideosUserService(this IServiceCollection services)
        {
            services.TryAddScoped<IUserService, UserService>();
        }

        public static void AddOnlineVideosImageService(this IServiceCollection services)
        {
            services.TryAddScoped<IImageService, ImageService>();
        }
    }
}
