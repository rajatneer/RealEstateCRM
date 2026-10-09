using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;

namespace RealEstateCRM.Services
{
    public interface IContactService
    {
        Task<List<Contact>> GetAllAsync();
        Task<List<Contact>> SearchAsync(string? query, ContactType? type);
        Task<Contact?> GetByIdAsync(int id);
        Task<Contact> CreateAsync(ContactCreateDto dto);
        Task<Contact?> UpdateAsync(int id, ContactUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class ContactService : IContactService
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<ContactService> _logger;
        private readonly IPagingContext _paging;

        public ContactService(CrmDbContext context, ILogger<ContactService> logger, IPagingContext paging)
        {
            _context = context;
            _logger = logger;
            _paging = paging;
        }

        public async Task<List<Contact>> GetAllAsync()
        {
            try
            {
                return await _context.Contacts
                    .OrderByDescending(c => c.CreatedAt)
                    .ToPagedListAsync(_paging);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving contacts");
                throw;
            }
        }

        public async Task<List<Contact>> SearchAsync(string? query, ContactType? type)
        {
            try
            {
                var q = _context.Contacts.AsQueryable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var term = query.Trim().ToLower();
                    q = q.Where(c => c.FirstName.ToLower().Contains(term)
                        || c.LastName.ToLower().Contains(term)
                        || (c.Email != null && c.Email.ToLower().Contains(term))
                        || (c.Phone != null && c.Phone.Contains(term)));
                }

                if (type.HasValue)
                    q = q.Where(c => c.Type == type.Value);

                return await q.OrderByDescending(c => c.CreatedAt).ToPagedListAsync(_paging);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error searching contacts");
                throw;
            }
        }

        public async Task<Contact?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Contacts
                    .Include(c => c.Interactions)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving contact {ContactId}", id);
                throw;
            }
        }

        public async Task<Contact> CreateAsync(ContactCreateDto dto)
        {
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var contact = new Contact
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Type = dto.Type,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created contact {ContactId}: {FirstName} {LastName}", contact.Id, contact.FirstName, contact.LastName);
                return contact;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error creating contact {FirstName} {LastName}", dto.FirstName, dto.LastName);
                throw;
            }
        }

        public async Task<Contact?> UpdateAsync(int id, ContactUpdateDto dto)
        {
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var contact = await _context.Contacts.FirstOrDefaultAsync(e => e.Id == id);
                if (contact == null) return null;

                contact.FirstName = dto.FirstName;
                contact.LastName = dto.LastName;
                contact.Email = dto.Email;
                contact.Phone = dto.Phone;
                contact.Type = dto.Type;
                contact.Notes = dto.Notes;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated contact {ContactId}", id);
                return contact;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error updating contact {ContactId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var contact = await _context.Contacts.FirstOrDefaultAsync(e => e.Id == id);
                if (contact == null) return false;

                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted contact {ContactId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error deleting contact {ContactId}", id);
                throw;
            }
        }
    }
}
