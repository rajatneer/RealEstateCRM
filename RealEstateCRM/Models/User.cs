using System.ComponentModel.DataAnnotations;

namespace RealEstateCRM.Models
{
    public static class Roles
    {
        public const string Owner = "Owner";
        public const string Agent = "Agent";

        public static readonly string[] All = { Owner, Agent };
    }

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

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = Roles.Agent;

        public bool IsActive { get; set; } = true;

        public int FailedLoginCount { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }
}
