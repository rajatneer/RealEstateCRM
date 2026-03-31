using System.ComponentModel.DataAnnotations;

namespace RealEstateCRM.Models
{
    public class CrmTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Required]
        public CrmTaskStatus Status { get; set; } = CrmTaskStatus.Pending;

        public int? ContactId { get; set; }
        public Contact? Contact { get; set; }

        public int? PropertyId { get; set; }
        public Property? Property { get; set; }

        public int? LeadId { get; set; }
        public Lead? Lead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum CrmTaskStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
}
