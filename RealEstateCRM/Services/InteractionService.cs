using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;

namespace RealEstateCRM.Services
{
    public interface IInteractionService
    {
        Task<List<Interaction>> GetAllAsync(int? contactId = null, int? propertyId = null);
        Task<Interaction?> GetByIdAsync(int id);
        Task<Interaction> CreateAsync(InteractionCreateDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class InteractionService : IInteractionService
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<InteractionService> _logger;

        public InteractionService(CrmDbContext context, ILogger<InteractionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Interaction>> GetAllAsync(int? contactId = null, int? propertyId = null)
        {
            try
            {
                var query = _context.Interactions
                    .Include(i => i.Contact)
                    .Include(i => i.Property)
                    .AsQueryable();

                if (contactId.HasValue)
                    query = query.Where(i => i.ContactId == contactId.Value);

                if (propertyId.HasValue)
                    query = query.Where(i => i.PropertyId == propertyId.Value);

                return await query
                    .OrderByDescending(i => i.Date)
                    .ToListAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving interactions");
                throw;
            }
        }

        public async Task<Interaction?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Interactions
                    .Include(i => i.Contact)
                    .Include(i => i.Property)
                    .FirstOrDefaultAsync(i => i.Id == id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving interaction {InteractionId}", id);
                throw;
            }
        }

        public async Task<Interaction> CreateAsync(InteractionCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var interaction = new Interaction
                {
                    ContactId = dto.ContactId,
                    PropertyId = dto.PropertyId,
                    Type = dto.Type,
                    Description = dto.Description,
                    Date = dto.Date ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Interactions.Add(interaction);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created interaction {InteractionId} for contact {ContactId}", interaction.Id, interaction.ContactId);
                return interaction;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error creating interaction for contact {ContactId}", dto.ContactId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var interaction = await _context.Interactions.FindAsync(id);
                if (interaction == null) return false;

                _context.Interactions.Remove(interaction);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted interaction {InteractionId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error deleting interaction {InteractionId}", id);
                throw;
            }
        }
    }
}
