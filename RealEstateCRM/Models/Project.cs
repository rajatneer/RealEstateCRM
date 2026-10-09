using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RealEstateCRM.Tenancy;

namespace RealEstateCRM.Models
{
    /// <summary>A villa/plot development at one location (e.g. "Green Valley Villas, Sector 12").</summary>
    public class Project : ITenantEntity
    {
        [Key]
        public int Id { get; set; }

        [JsonIgnore]
        public int CompanyId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        public int TotalVillas { get; set; }

        [Required]
        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProjectMilestone> Milestones { get; set; } = new List<ProjectMilestone>();
    }

    public enum ProjectStatus
    {
        Planning,
        Approvals,
        UnderConstruction,
        Completed,
        OnHold
    }

    /// <summary>A dated step of a project: agreement, registry, an approval, or a construction stage.</summary>
    public class ProjectMilestone : ITenantEntity
    {
        [Key]
        public int Id { get; set; }

        [JsonIgnore]
        public int CompanyId { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required]
        public MilestoneCategory Category { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Deadline / planned date (date only).</summary>
        public DateTime PlannedDate { get; set; }

        /// <summary>Actual completion date, set when the step is done.</summary>
        public DateTime? CompletedDate { get; set; }

        [Required]
        public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;

        /// <summary>File / application / registry number for the step, if any.</summary>
        [MaxLength(100)]
        public string? ReferenceNo { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum MilestoneCategory
    {
        Agreement,
        Registry,
        Approval,
        Construction,
        Other
    }

    public enum MilestoneStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
}
