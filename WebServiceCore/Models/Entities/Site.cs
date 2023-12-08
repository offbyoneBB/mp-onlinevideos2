using System.ComponentModel.DataAnnotations;

namespace WebServiceCore.Models.Entities
{
    public enum SiteState : byte { Working, Reported, Broken };

    public class Site
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        [Key]
        public string Name { get; set; }

        public string XML { get; set; }
        public SiteState State { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
        public bool IsAdult { get; set; }

        // Navigation properties that store relationships to other entities
        // EF Core will automatically use a property suffixed with Id as the
        // foreign key for the navigation property of the same name

        public string OwnerId { get; set; }
        public User Owner { get; set; }

        public string DllId { get; set; }
        public Dll Dll { get; set; }

        public List<Report> Reports { get; set; }
    }
}
