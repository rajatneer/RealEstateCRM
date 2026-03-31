using System.ComponentModel.DataAnnotations;

namespace RealEstateCRM.Models
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty; // Unique company code

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
