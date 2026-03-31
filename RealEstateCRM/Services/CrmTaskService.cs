using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;

namespace RealEstateCRM.Services
{
    public interface ICrmTaskService
    {
        Task<List<CrmTask>> GetAllAsync(CrmTaskStatus? status = null);
        Task<CrmTask?> GetByIdAsync(int id);
        Task<CrmTask> CreateAsync(CrmTaskCreateDto dto);
        Task<CrmTask?> UpdateAsync(int id, CrmTaskUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<CrmTask>> GetOverdueAsync();
    }

    public class CrmTaskService : ICrmTaskService
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<CrmTaskService> _logger;

        public CrmTaskService(CrmDbContext context, ILogger<CrmTaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CrmTask>> GetAllAsync(CrmTaskStatus? status = null)
        {
            try
            {
                var query = _context.CrmTasks
                    .Include(t => t.Contact)
                    .Include(t => t.Property)
                    .Include(t => t.Lead)
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(t => t.Status == status.Value);

                return await query
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving tasks");
                throw;
            }
        }

        public async Task<CrmTask?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.CrmTasks
                    .Include(t => t.Contact)
                    .Include(t => t.Property)
                    .Include(t => t.Lead)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving task {TaskId}", id);
                throw;
            }
        }

        public async Task<CrmTask> CreateAsync(CrmTaskCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var task = new CrmTask
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    DueDate = dto.DueDate,
                    Priority = dto.Priority,
                    Status = CrmTaskStatus.Pending,
                    ContactId = dto.ContactId,
                    PropertyId = dto.PropertyId,
                    LeadId = dto.LeadId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CrmTasks.Add(task);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created task {TaskId}: {Title}", task.Id, task.Title);
                return task;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error creating task {Title}", dto.Title);
                throw;
            }
        }

        public async Task<CrmTask?> UpdateAsync(int id, CrmTaskUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var task = await _context.CrmTasks.FindAsync(id);
                if (task == null) return null;

                task.Title = dto.Title;
                task.Description = dto.Description;
                task.DueDate = dto.DueDate;
                task.Priority = dto.Priority;
                task.Status = dto.Status;
                task.ContactId = dto.ContactId;
                task.PropertyId = dto.PropertyId;
                task.LeadId = dto.LeadId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated task {TaskId}", id);
                return task;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error updating task {TaskId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var task = await _context.CrmTasks.FindAsync(id);
                if (task == null) return false;

                _context.CrmTasks.Remove(task);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted task {TaskId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error deleting task {TaskId}", id);
                throw;
            }
        }

        public async Task<List<CrmTask>> GetOverdueAsync()
        {
            try
            {
                return await _context.CrmTasks
                    .Include(t => t.Contact)
                    .Include(t => t.Property)
                    .Where(t => t.DueDate < DateTime.UtcNow && t.Status != CrmTaskStatus.Completed && t.Status != CrmTaskStatus.Cancelled)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving overdue tasks");
                throw;
            }
        }
    }
}
