using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;

namespace RealEstateCRM.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetAllAsync(ProjectStatus? status = null);
        Task<Project?> GetByIdAsync(int id);
        Task<Project> CreateAsync(ProjectCreateDto dto);
        Task<Project?> UpdateAsync(int id, ProjectUpdateDto dto);
        Task<bool> DeleteAsync(int id);

        Task<ProjectMilestone> AddMilestoneAsync(int projectId, MilestoneCreateDto dto);
        Task<ProjectMilestone?> UpdateMilestoneAsync(int milestoneId, MilestoneUpdateDto dto);
        Task<ProjectMilestone?> CompleteMilestoneAsync(int milestoneId, DateTime? completedDate);
        Task<bool> DeleteMilestoneAsync(int milestoneId);

        /// <summary>Open milestones that are overdue or due within the next <paramref name="days"/> days, across all projects.</summary>
        Task<List<ProjectMilestone>> GetUpcomingAsync(int days);
    }

    public class ProjectService : IProjectService
    {
        /// <summary>Starting step list; dates are offsets (days) from the project start date and meant to be edited.</summary>
        private static readonly (MilestoneCategory Category, string Title, int Days)[] DefaultSteps =
        {
            (MilestoneCategory.Agreement,    "Agreement / MOU signing",            15),
            (MilestoneCategory.Registry,     "Land registry",                      30),
            (MilestoneCategory.Approval,     "TCP approval",                       60),
            (MilestoneCategory.Approval,     "Panchayat approval",                 75),
            (MilestoneCategory.Construction, "Site clearing & layout marking",     90),
            (MilestoneCategory.Construction, "Foundation",                        120),
            (MilestoneCategory.Construction, "Plinth",                            150),
            (MilestoneCategory.Construction, "Structure / slab",                  210),
            (MilestoneCategory.Construction, "Brickwork",                         250),
            (MilestoneCategory.Construction, "Plaster",                           290),
            (MilestoneCategory.Construction, "Flooring & electrical",             330),
            (MilestoneCategory.Construction, "Finishing & painting",              380),
            (MilestoneCategory.Other,        "Handover",                          420),
        };

        private readonly CrmDbContext _context;
        private readonly ILogger<ProjectService> _logger;
        private readonly IPagingContext _paging;

        public ProjectService(CrmDbContext context, ILogger<ProjectService> logger, IPagingContext paging)
        {
            _context = context;
            _logger = logger;
            _paging = paging;
        }

        public Task<List<Project>> GetAllAsync(ProjectStatus? status = null)
        {
            var query = _context.Projects.Include(p => p.Milestones).AsQueryable();
            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);
            return query.OrderByDescending(p => p.CreatedAt).ToPagedListAsync(_paging);
        }

        public Task<Project?> GetByIdAsync(int id) =>
            _context.Projects.Include(p => p.Milestones).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Project> CreateAsync(ProjectCreateDto dto)
        {
            var project = new Project
            {
                Name = dto.Name.Trim(),
                Location = dto.Location.Trim(),
                City = dto.City,
                State = dto.State,
                TotalVillas = dto.TotalVillas,
                Status = dto.Status,
                StartDate = dto.StartDate,
                ExpectedCompletionDate = dto.ExpectedCompletionDate,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            if (dto.AddDefaultMilestones)
            {
                var start = (dto.StartDate ?? DateTime.UtcNow).Date;
                foreach (var (category, title, days) in DefaultSteps)
                {
                    project.Milestones.Add(new ProjectMilestone
                    {
                        Category = category,
                        Title = title,
                        PlannedDate = start.AddDays(days),
                        Status = MilestoneStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created project {ProjectId}: {Name}", project.Id, project.Name);
            return project;
        }

        public async Task<Project?> UpdateAsync(int id, ProjectUpdateDto dto)
        {
            var project = await _context.Projects.Include(p => p.Milestones).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return null;

            project.Name = dto.Name.Trim();
            project.Location = dto.Location.Trim();
            project.City = dto.City;
            project.State = dto.State;
            project.TotalVillas = dto.TotalVillas;
            project.Status = dto.Status;
            project.StartDate = dto.StartDate;
            project.ExpectedCompletionDate = dto.ExpectedCompletionDate;
            project.Notes = dto.Notes;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated project {ProjectId}", id);
            return project;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return false;

            _context.Projects.Remove(project); // milestones cascade
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted project {ProjectId}", id);
            return true;
        }

        public async Task<ProjectMilestone> AddMilestoneAsync(int projectId, MilestoneCreateDto dto)
        {
            await _context.EnsureExistsAsync<Project>(projectId, "Project");

            var milestone = new ProjectMilestone
            {
                ProjectId = projectId,
                CreatedAt = DateTime.UtcNow
            };
            Apply(milestone, dto);

            _context.ProjectMilestones.Add(milestone);
            await _context.SaveChangesAsync();
            return milestone;
        }

        public async Task<ProjectMilestone?> UpdateMilestoneAsync(int milestoneId, MilestoneUpdateDto dto)
        {
            var milestone = await _context.ProjectMilestones.FirstOrDefaultAsync(m => m.Id == milestoneId);
            if (milestone == null) return null;

            Apply(milestone, dto);
            await _context.SaveChangesAsync();
            return milestone;
        }

        public async Task<ProjectMilestone?> CompleteMilestoneAsync(int milestoneId, DateTime? completedDate)
        {
            var milestone = await _context.ProjectMilestones.FirstOrDefaultAsync(m => m.Id == milestoneId);
            if (milestone == null) return null;

            milestone.Status = MilestoneStatus.Completed;
            milestone.CompletedDate = (completedDate ?? DateTime.UtcNow).Date;
            await _context.SaveChangesAsync();
            return milestone;
        }

        public async Task<bool> DeleteMilestoneAsync(int milestoneId)
        {
            var milestone = await _context.ProjectMilestones.FirstOrDefaultAsync(m => m.Id == milestoneId);
            if (milestone == null) return false;

            _context.ProjectMilestones.Remove(milestone);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<List<ProjectMilestone>> GetUpcomingAsync(int days)
        {
            var until = DateTime.UtcNow.Date.AddDays(days);
            return _context.ProjectMilestones
                .Include(m => m.Project)
                .Where(m => (m.Status == MilestoneStatus.Pending || m.Status == MilestoneStatus.InProgress) && m.PlannedDate <= until)
                .OrderBy(m => m.PlannedDate)
                .ToPagedListAsync(_paging);
        }

        private static void Apply(ProjectMilestone milestone, MilestoneCreateDto dto)
        {
            milestone.Category = dto.Category;
            milestone.Title = dto.Title.Trim();
            milestone.PlannedDate = dto.PlannedDate.Date;
            milestone.Status = dto.Status;
            milestone.ReferenceNo = dto.ReferenceNo;
            milestone.Notes = dto.Notes;
            milestone.CompletedDate = dto.Status == MilestoneStatus.Completed
                ? (dto.CompletedDate ?? DateTime.UtcNow).Date
                : null;
        }
    }
}
