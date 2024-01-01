using System.ComponentModel.DataAnnotations;

namespace WebServiceCore.Models.Entities
{
    public class Dll
    {
        [Key]
        public string Name { get; set; }
        public DateTime LastUpdated { get; set; }
        public string MD5 { get; set; }
        public string OwnerId { get; set; }
        public User Owner { get; set; }
    }
}
