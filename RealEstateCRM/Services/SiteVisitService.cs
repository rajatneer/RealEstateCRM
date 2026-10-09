using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;

namespace RealEstateCRM.Services
{
    public interface ISiteVisitService
    {
        Task<List<SiteVisit>> GetAllAsync(SiteVisitStatus? status = null);
        Task<SiteVisit?> GetByIdAsync(int id);
        Task<SiteVisit> CreateAsync(SiteVisitCreateDto dto);
        Task<SiteVisit?> UpdateAsync(int id, SiteVisitUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<SiteVisit>> GetUpcomingAsync();
    }

    public class SiteVisitService : ISiteVisitService
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<SiteVisitService> _logger;
        private readonly IPagingContext _paging;

        public SiteVisitService(CrmDbContext context, ILogger<SiteVisitService> logger, IPagingContext paging)
        {
            _context = context;
            _logger = logger;
            _paging = paging;
        }

        public async Task<List<SiteVisit>> GetAllAsync(SiteVisitStatus? status = null)
        {
            try
            {
                var query = _context.SiteVisits
                    .Include(s => s.Contact)
                    .Include(s => s.Property)
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(s => s.Status == status.Value);

                return await query.OrderByDescending(s => s.ScheduledDate).ToPagedListAsync(_paging);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving site visits");
                throw;
            }
        }

        public async Task<SiteVisit?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.SiteVisits
                    .Include(s => s.Contact)
                    .Include(s => s.Property)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving site visit {SiteVisitId}", id);
                throw;
            }
        }

        public async Task<SiteVisit> CreateAsync(SiteVisitCreateDto dto)
        {
            await _context.EnsureExistsAsync<Contact>(dto.ContactId, "Contact");
            await _context.EnsureExistsAsync<Property>(dto.PropertyId, "Property");
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var siteVisit = new SiteVisit
                {
                    ContactId = dto.ContactId,
                    PropertyId = dto.PropertyId,
                    ScheduledDate = dto.ScheduledDate,
                    Status = SiteVisitStatus.Scheduled,
                    PickupLocation = dto.PickupLocation,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SiteVisits.Add(siteVisit);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created site visit {SiteVisitId} for contact {ContactId}", siteVisit.Id, siteVisit.ContactId);
                return siteVisit;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error creating site visit for contact {ContactId}", dto.ContactId);
                throw;
            }
        }

        public async Task<SiteVisit?> UpdateAsync(int id, SiteVisitUpdateDto dto)
        {
            await _context.EnsureExistsAsync<Contact>(dto.ContactId, "Contact");
            await _context.EnsureExistsAsync<Property>(dto.PropertyId, "Property");
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var siteVisit = await _context.SiteVisits.FirstOrDefaultAsync(e => e.Id == id);
                if (siteVisit == null) return null;

                siteVisit.ContactId = dto.ContactId;
                siteVisit.PropertyId = dto.PropertyId;
                siteVisit.ScheduledDate = dto.ScheduledDate;
                siteVisit.Status = dto.Status;
                siteVisit.PickupLocation = dto.PickupLocation;
                siteVisit.Feedback = dto.Feedback;
                siteVisit.Notes = dto.Notes;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated site visit {SiteVisitId}", id);
                return siteVisit;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error updating site visit {SiteVisitId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var siteVisit = await _context.SiteVisits.FirstOrDefaultAsync(e => e.Id == id);
                if (siteVisit == null) return false;

                _context.SiteVisits.Remove(siteVisit);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted site visit {SiteVisitId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error deleting site visit {SiteVisitId}", id);
                throw;
            }
        }

        public async Task<List<SiteVisit>> GetUpcomingAsync()
        {
            try
            {
                return await _context.SiteVisits
                    .Include(s => s.Contact)
                    .Include(s => s.Property)
                    .Where(s => s.Status == SiteVisitStatus.Scheduled || s.Status == SiteVisitStatus.Confirmed)
                    .Where(s => s.ScheduledDate >= DateTime.UtcNow)
                    .OrderBy(s => s.ScheduledDate)
                    .ToPagedListAsync(_paging);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving upcoming site visits");
                throw;
            }
        }
    }
}
