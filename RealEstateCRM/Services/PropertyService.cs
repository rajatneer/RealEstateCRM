using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;

namespace RealEstateCRM.Services
{
    public interface IPropertyService
    {
        Task<List<Property>> GetAllAsync(PropertyStatus? status = null);
        Task<List<Property>> SearchAsync(string? query, PropertyType? type, PropertyStatus? status, decimal? minPrice, decimal? maxPrice, int? minBeds);
        Task<Property?> GetByIdAsync(int id);
        Task<Property> CreateAsync(PropertyCreateDto dto);
        Task<Property?> UpdateAsync(int id, PropertyUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class PropertyService : IPropertyService
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<PropertyService> _logger;

        public PropertyService(CrmDbContext context, ILogger<PropertyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Property>> GetAllAsync(PropertyStatus? status = null)
        {
            try
            {
                var query = _context.Properties
                    .Include(p => p.Owner)
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(p => p.Status == status.Value);

                return await query
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving properties");
                throw;
            }
        }

        public async Task<List<Property>> SearchAsync(string? query, PropertyType? type, PropertyStatus? status, decimal? minPrice, decimal? maxPrice, int? minBeds)
        {
            try
            {
                var q = _context.Properties.Include(p => p.Owner).AsQueryable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var term = query.Trim().ToLower();
                    q = q.Where(p => p.Address.ToLower().Contains(term)
                        || p.City.ToLower().Contains(term)
                        || p.State.ToLower().Contains(term)
                        || (p.ZipCode != null && p.ZipCode.Contains(term)));
                }

                if (type.HasValue)
                    q = q.Where(p => p.Type == type.Value);
                if (status.HasValue)
                    q = q.Where(p => p.Status == status.Value);
                if (minPrice.HasValue)
                    q = q.Where(p => p.Price >= minPrice.Value);
                if (maxPrice.HasValue)
                    q = q.Where(p => p.Price <= maxPrice.Value);
                if (minBeds.HasValue)
                    q = q.Where(p => p.Bedrooms >= minBeds.Value);

                return await q.OrderByDescending(p => p.CreatedAt).ToListAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error searching properties");
                throw;
            }
        }

        public async Task<Property?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Interactions)
                        .ThenInclude(i => i.Contact)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving property {PropertyId}", id);
                throw;
            }
        }

        public async Task<Property> CreateAsync(PropertyCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var property = new Property
                {
                    Address = dto.Address,
                    City = dto.City,
                    State = dto.State,
                    ZipCode = dto.ZipCode,
                    Type = dto.Type,
                    Status = dto.Status,
                    Price = dto.Price,
                    Bedrooms = dto.Bedrooms,
                    Bathrooms = dto.Bathrooms,
                    SquareFeet = dto.SquareFeet,
                    Description = dto.Description,
                    OwnerId = dto.OwnerId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created property {PropertyId}: {Address}", property.Id, property.Address);
                return property;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error creating property {Address}", dto.Address);
                throw;
            }
        }

        public async Task<Property?> UpdateAsync(int id, PropertyUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var property = await _context.Properties.FindAsync(id);
                if (property == null) return null;

                property.Address = dto.Address;
                property.City = dto.City;
                property.State = dto.State;
                property.ZipCode = dto.ZipCode;
                property.Type = dto.Type;
                property.Status = dto.Status;
                property.Price = dto.Price;
                property.Bedrooms = dto.Bedrooms;
                property.Bathrooms = dto.Bathrooms;
                property.SquareFeet = dto.SquareFeet;
                property.Description = dto.Description;
                property.OwnerId = dto.OwnerId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated property {PropertyId}", id);
                return property;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error updating property {PropertyId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var property = await _context.Properties.FindAsync(id);
                if (property == null) return false;

                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted property {PropertyId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error deleting property {PropertyId}", id);
                throw;
            }
        }
    }
}
