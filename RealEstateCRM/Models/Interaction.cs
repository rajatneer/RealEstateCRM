using System.Text.Json.Serialization;
using RealEstateCRM.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace RealEstateCRM.Models
{
    public class Interaction : ITenantEntity, IOwnedEntity
    {
        [Key]
        public int Id { get; set; }

        [JsonIgnore]
        public int CompanyId { get; set; }

        [JsonIgnore]
        public int? AssignedUserId { get; set; }

        [Required]
        public int ContactId { get; set; }
        public Contact Contact { get; set; } = null!;

        public int? PropertyId { get; set; }
        public Property? Property { get; set; }

        [Required]
        public InteractionType Type { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum InteractionType
    {
        Call,
        Email,
        Meeting,
        Showing,
        Offer,
        Other
    }
}
