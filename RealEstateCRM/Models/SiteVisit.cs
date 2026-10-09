using System.Text.Json.Serialization;
using RealEstateCRM.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace RealEstateCRM.Models
{
    public class SiteVisit : ITenantEntity
    {
        [Key]
        public int Id { get; set; }

        [JsonIgnore]
        public int CompanyId { get; set; }

        [Required]
        public int ContactId { get; set; }
        public Contact Contact { get; set; } = null!;

        [Required]
        public int PropertyId { get; set; }
        public Property Property { get; set; } = null!;

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        public SiteVisitStatus Status { get; set; } = SiteVisitStatus.Scheduled;

        [MaxLength(200)]
        public string? PickupLocation { get; set; }

        [MaxLength(500)]
        public string? Feedback { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum SiteVisitStatus
    {
        Scheduled,
        Confirmed,
        Completed,
        Cancelled,
        NoShow
    }
}
