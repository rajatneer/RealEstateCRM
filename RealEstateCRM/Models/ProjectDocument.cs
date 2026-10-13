using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RealEstateCRM.Tenancy;

namespace RealEstateCRM.Models
{
    /// <summary>A paper-work file (PDF, photo, sheet...) attached to a project, optionally tied to one of its stages.
    /// The bytes live in the database so they survive restarts on hosts without a persistent disk.</summary>
    public class ProjectDocument : ITenantEntity
    {
        [Key]
        public int Id { get; set; }

        [JsonIgnore]
        public int CompanyId { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        /// <summary>Stage (milestone) this file belongs to; null = the whole project.</summary>
        public int? MilestoneId { get; set; }

        [Required, MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ContentType { get; set; } = "application/octet-stream";

        public long SizeBytes { get; set; }

        [JsonIgnore]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [MaxLength(500)]
        public string? Note { get; set; }

        [MaxLength(100)]
        public string? UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
