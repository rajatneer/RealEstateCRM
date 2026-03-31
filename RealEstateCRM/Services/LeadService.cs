using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;


namespace RealEstateCRM.Services
{

    public interface ILeadService
    {
        Task<List<TimelineEntryDto>?> GetTimelineAsync(int leadId);
        Task<List<Lead>> GetAllAsync(LeadStage? stage = null);
        Task<Lead?> GetByIdAsync(int id);
        Task<Lead> CreateAsync(LeadCreateDto dto);
        Task<Lead?> UpdateAsync(int id, LeadUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class LeadService : ILeadService
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<LeadService> _logger;

        public LeadService(CrmDbContext context, ILogger<LeadService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Lead>> GetAllAsync(LeadStage? stage = null)
        {
            try
            {
                var query = _context.Leads
                    .Include(l => l.Contact)
                    .Include(l => l.Property)
                    .AsQueryable();

                if (stage.HasValue)
                    query = query.Where(l => l.Stage == stage.Value);

                return await query
                    .OrderByDescending(l => l.UpdatedAt)
                    .ToListAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving leads");
                throw;
            }
        }

        public async Task<Lead?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Leads
                    .Include(l => l.Contact)
                    .Include(l => l.Property)
                    .FirstOrDefaultAsync(l => l.Id == id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving lead {LeadId}", id);
                throw;
            }
        }

        public async Task<Lead> CreateAsync(LeadCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var lead = new Lead
                {
                    ContactId = dto.ContactId,
                    PropertyId = dto.PropertyId,
                    Stage = dto.Stage,
                    Source = dto.Source,
                    EstimatedValue = dto.EstimatedValue,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Leads.Add(lead);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created lead {LeadId} for contact {ContactId}", lead.Id, lead.ContactId);
                return lead;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error creating lead for contact {ContactId}", dto.ContactId);
                throw;
            }
        }

        public async Task<Lead?> UpdateAsync(int id, LeadUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var lead = await _context.Leads.FindAsync(id);
                if (lead == null) return null;

                lead.ContactId = dto.ContactId;
                lead.PropertyId = dto.PropertyId;
                lead.Stage = dto.Stage;
                lead.Source = dto.Source;
                lead.EstimatedValue = dto.EstimatedValue;
                lead.Notes = dto.Notes;
                lead.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated lead {LeadId} to stage {Stage}", id, dto.Stage);
                return lead;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error updating lead {LeadId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var lead = await _context.Leads.FindAsync(id);
                if (lead == null) return false;

                _context.Leads.Remove(lead);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted lead {LeadId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error deleting lead {LeadId}", id);
                throw;
            }
        }

        public async Task<List<TimelineEntryDto>?> GetTimelineAsync(int leadId)
        {
            try
            {
                var lead = await _context.Leads.FindAsync(leadId);
                if (lead == null) return null;

                var contactId = lead.ContactId;
                var propertyId = lead.PropertyId;

                var interactions = await _context.Interactions
                    .Where(i => i.ContactId == contactId || (propertyId != null && i.PropertyId == propertyId))
                    .Select(i => new TimelineEntryDto
                    {
                        Type = "Interaction",
                        Date = i.Date,
                        Summary = i.Type.ToString(),
                        Details = i.Description
                    }).ToListAsync();

                var tasks = await _context.CrmTasks
                    .Where(t => t.LeadId == leadId)
                    .Select(t => new TimelineEntryDto
                    {
                        Type = "Task",
                        Date = t.DueDate,
                        Summary = t.Title,
                        Details = t.Description ?? string.Empty
                    }).ToListAsync();

                var siteVisits = await _context.SiteVisits
                    .Where(s => s.ContactId == contactId && (propertyId == null || s.PropertyId == propertyId))
                    .Select(s => new TimelineEntryDto
                    {
                        Type = "SiteVisit",
                        Date = s.ScheduledDate,
                        Summary = s.Status.ToString(),
                        Details = s.Notes ?? string.Empty
                    }).ToListAsync();

                var brokerages = await _context.Brokerages
                    .Where(b => b.LeadId == leadId)
                    .Select(b => new TimelineEntryDto
                    {
                        Type = "Brokerage",
                        Date = b.CreatedAt,
                        Summary = $"Deal: {b.DealValue:C} Status: {b.PaymentStatus}",
                        Details = b.Notes ?? string.Empty
                    }).ToListAsync();

                var timeline = interactions
                    .Concat(tasks)
                    .Concat(siteVisits)
                    .Concat(brokerages)
                    .OrderByDescending(e => e.Date)
                    .ToList();

                return timeline;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating timeline for lead {LeadId}", leadId);
                throw;
            }
        }
    }
}
