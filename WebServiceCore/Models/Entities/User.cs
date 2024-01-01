using System.ComponentModel.DataAnnotations;

namespace WebServiceCore.Models.Entities
{
    public class User
    {
        [Key]
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }
        public List<Site> Sites { get; set; }
        public List<Dll> Dlls { get; set; }
    }
}
