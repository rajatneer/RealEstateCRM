using System.ComponentModel.DataAnnotations;

namespace RealEstateCRM.Models.DTOs
{
    // ── Requests ──
    public class ProjectCreateDto
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Location { get; set; } = string.Empty;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [Range(0, 100000)]
        public int TotalVillas { get; set; }

        [EnumDataType(typeof(ProjectStatus))]
        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }

        [StringLength(100)]
        public string? ProjectManager { get; set; }

        [StringLength(100)]
        public string? EscalationOwner { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>On create only: copy every stage from the Stages page (owner, order, default time) into this project.</summary>
        public bool AddDefaultMilestones { get; set; }
    }

    public class ProjectUpdateDto : ProjectCreateDto { }

    public class MilestoneCreateDto
    {
        [EnumDataType(typeof(MilestoneCategory))]
        public MilestoneCategory Category { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Owner { get; set; }

        public DateTime PlannedDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        [EnumDataType(typeof(MilestoneStatus))]
        public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;

        [StringLength(100)]
        public string? ReferenceNo { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class MilestoneUpdateDto : MilestoneCreateDto { }

    public class CompleteMilestoneDto
    {
        /// <summary>Defaults to today when omitted.</summary>
        public DateTime? CompletedDate { get; set; }
    }

    // ── Responses ──
    public class MilestoneDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public MilestoneCategory Category { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Owner { get; set; }
        public DateTime PlannedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public MilestoneStatus Status { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Notes { get; set; }
        public bool IsOverdue { get; set; }
        /// <summary>Days until the planned date (negative = days late). Null once completed or cancelled.</summary>
        public int? DaysRemaining { get; set; }
    }

    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? State { get; set; }
        public int TotalVillas { get; set; }
        public ProjectStatus Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public string? ProjectManager { get; set; }
        public string? EscalationOwner { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalMilestones { get; set; }
        public int CompletedMilestones { get; set; }
        public int OverdueMilestones { get; set; }
        public int ProgressPercent { get; set; }
        public MilestoneDto? NextMilestone { get; set; }
        public List<MilestoneDto> Milestones { get; set; } = new();
    }

    public static class ProjectMappings
    {
        private static bool IsOpen(ProjectMilestone m) =>
            m.Status == MilestoneStatus.Pending || m.Status == MilestoneStatus.InProgress;

        public static MilestoneDto ToDto(this ProjectMilestone m, DateTime today, bool includeProjectName = false)
        {
            var open = IsOpen(m);
            var days = open ? (int?)(m.PlannedDate.Date - today.Date).TotalDays : null;
            return new MilestoneDto
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                ProjectName = includeProjectName ? m.Project?.Name : null,
                Category = m.Category,
                Title = m.Title,
                Owner = m.Owner,
                PlannedDate = m.PlannedDate,
                CompletedDate = m.CompletedDate,
                Status = m.Status,
                ReferenceNo = m.ReferenceNo,
                Notes = m.Notes,
                IsOverdue = open && days < 0,
                DaysRemaining = days
            };
        }

        /// <summary>Project status follows its stages: nothing started = Planning, permits under way = Approvals,
        /// construction under way = UnderConstruction, everything done = Completed. A project put on hold by hand stays OnHold.</summary>
        private static ProjectStatus DeriveStatus(Project p, List<MilestoneDto> ms)
        {
            if (p.Status == ProjectStatus.OnHold) return ProjectStatus.OnHold;
            var active = ms.Where(m => m.Status != MilestoneStatus.Cancelled).ToList();
            if (active.Count == 0) return ProjectStatus.Planning;
            if (active.All(m => m.Status == MilestoneStatus.Completed)) return ProjectStatus.Completed;
            bool Started(MilestoneDto m) => m.Status == MilestoneStatus.Completed || m.Status == MilestoneStatus.InProgress;
            if (active.Any(m => m.Category == MilestoneCategory.Construction && Started(m))) return ProjectStatus.UnderConstruction;
            if (active.Any(m => m.Category == MilestoneCategory.Approval && Started(m))) return ProjectStatus.Approvals;
            return ProjectStatus.Planning;
        }

        public static ProjectDto ToDto(this Project p, DateTime today)
        {
            var milestones = p.Milestones.OrderBy(m => m.PlannedDate).ThenBy(m => m.Sequence).ThenBy(m => m.Id).Select(m => m.ToDto(today)).ToList();
            var counted = milestones.Where(m => m.Status != MilestoneStatus.Cancelled).ToList();
            var done = counted.Count(m => m.Status == MilestoneStatus.Completed);

            return new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Location = p.Location,
                City = p.City,
                State = p.State,
                TotalVillas = p.TotalVillas,
                Status = DeriveStatus(p, milestones),
                StartDate = p.StartDate,
                ExpectedCompletionDate = p.ExpectedCompletionDate,
                ProjectManager = p.ProjectManager,
                EscalationOwner = p.EscalationOwner,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                TotalMilestones = counted.Count,
                CompletedMilestones = done,
                OverdueMilestones = counted.Count(m => m.IsOverdue),
                ProgressPercent = counted.Count == 0 ? 0 : (int)Math.Round(100.0 * done / counted.Count),
                NextMilestone = milestones.FirstOrDefault(m => m.Status == MilestoneStatus.Pending || m.Status == MilestoneStatus.InProgress),
                Milestones = milestones
            };
        }
    }
}
