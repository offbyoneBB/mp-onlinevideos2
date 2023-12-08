using Microsoft.EntityFrameworkCore;
using WebServiceCore.Models.Entities;

namespace WebServiceCore.Models
{
    public class OnlineVideosDataContext : DbContext
    {
        public OnlineVideosDataContext(DbContextOptions<OnlineVideosDataContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<Dll> Dlls { get; set; }
        public DbSet<Report> Reports { get; set; }
    }
}
