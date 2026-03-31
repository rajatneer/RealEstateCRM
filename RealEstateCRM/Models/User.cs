using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateCRM.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
