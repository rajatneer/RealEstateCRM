using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateCRM.Models
{
    public class Lead
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ContactId { get; set; }
        public Contact Contact { get; set; } = null!;

        public int? PropertyId { get; set; }
        public Property? Property { get; set; }

        [Required]
        public LeadStage Stage { get; set; } = LeadStage.New;

        [Required]
        public LeadSource Source { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedValue { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum LeadStage
    {
        New,
        Contacted,
        Qualified,
        Negotiation,
        Won,
        Lost
    }

    public enum LeadSource
    {
        Website,
        Referral,
        Advertisement,
        WalkIn,
        SocialMedia,
        ColdCall,
        Other
    }
}
