using System.ComponentModel.DataAnnotations;

namespace RealEstateCRM.Models
{
    public class Contact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required]
        public ContactType Type { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
    }

    public enum ContactType
    {
        Buyer,
        Seller,
        Tenant,
        Landlord,
        Agent
    }
}
