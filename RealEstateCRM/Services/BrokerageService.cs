using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;

namespace RealEstateCRM.Services
{
    public interface IBrokerageService
    {
        Task<List<Brokerage>> GetAllAsync(BrokeragePaymentStatus? status = null);
        Task<Brokerage?> GetByIdAsync(int id);
        Task<Brokerage> CreateAsync(BrokerageCreateDto dto);
        Task<Brokerage?> UpdateAsync(int id, BrokerageUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class BrokerageService : IBrokerageService
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<BrokerageService> _logger;

        public BrokerageService(CrmDbContext context, ILogger<BrokerageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Brokerage>> GetAllAsync(BrokeragePaymentStatus? status = null)
        {
            try
            {
                var query = _context.Brokerages
                    .Include(b => b.Lead).ThenInclude(l => l!.Contact)
                    .Include(b => b.Property)
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(b => b.PaymentStatus == status.Value);

                return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving brokerages");
                throw;
            }
        }

        public async Task<Brokerage?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Brokerages
                    .Include(b => b.Lead).ThenInclude(l => l!.Contact)
                    .Include(b => b.Property)
                    .FirstOrDefaultAsync(b => b.Id == id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving brokerage {BrokerageId}", id);
                throw;
            }
        }

        public async Task<Brokerage> CreateAsync(BrokerageCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var commissionAmount = dto.DealValue * dto.CommissionPercent / 100m;
                var gstPercent = 18.00m;
                var gstAmount = commissionAmount * gstPercent / 100m;
                var totalPayable = commissionAmount + gstAmount;

                decimal? subBrokerAmount = null;
                if (dto.SubBrokerSplitPercent.HasValue && dto.SubBrokerSplitPercent > 0)
                {
                    subBrokerAmount = commissionAmount * dto.SubBrokerSplitPercent.Value / 100m;
                }

                var brokerage = new Brokerage
                {
                    LeadId = dto.LeadId,
                    PropertyId = dto.PropertyId,
                    DealValue = dto.DealValue,
                    CommissionPercent = dto.CommissionPercent,
                    CommissionAmount = commissionAmount,
                    GstPercent = gstPercent,
                    GstAmount = gstAmount,
                    TotalPayable = totalPayable,
                    SubBrokerName = dto.SubBrokerName,
                    SubBrokerSplitPercent = dto.SubBrokerSplitPercent,
                    SubBrokerAmount = subBrokerAmount,
                    PaymentStatus = BrokeragePaymentStatus.Pending,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Brokerages.Add(brokerage);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created brokerage {BrokerageId} for deal value {DealValue}", brokerage.Id, brokerage.DealValue);
                return brokerage;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error creating brokerage");
                throw;
            }
        }

        public async Task<Brokerage?> UpdateAsync(int id, BrokerageUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var brokerage = await _context.Brokerages.FindAsync(id);
                if (brokerage == null) return null;

                var commissionAmount = dto.DealValue * dto.CommissionPercent / 100m;
                var gstAmount = commissionAmount * brokerage.GstPercent / 100m;
                var totalPayable = commissionAmount + gstAmount;

                decimal? subBrokerAmount = null;
                if (dto.SubBrokerSplitPercent.HasValue && dto.SubBrokerSplitPercent > 0)
                {
                    subBrokerAmount = commissionAmount * dto.SubBrokerSplitPercent.Value / 100m;
                }

                brokerage.LeadId = dto.LeadId;
                brokerage.PropertyId = dto.PropertyId;
                brokerage.DealValue = dto.DealValue;
                brokerage.CommissionPercent = dto.CommissionPercent;
                brokerage.CommissionAmount = commissionAmount;
                brokerage.GstAmount = gstAmount;
                brokerage.TotalPayable = totalPayable;
                brokerage.SubBrokerName = dto.SubBrokerName;
                brokerage.SubBrokerSplitPercent = dto.SubBrokerSplitPercent;
                brokerage.SubBrokerAmount = subBrokerAmount;
                brokerage.PaymentStatus = dto.PaymentStatus;
                brokerage.Notes = dto.Notes;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated brokerage {BrokerageId}", id);
                return brokerage;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error updating brokerage {BrokerageId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var brokerage = await _context.Brokerages.FindAsync(id);
                if (brokerage == null) return false;

                _context.Brokerages.Remove(brokerage);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted brokerage {BrokerageId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error deleting brokerage {BrokerageId}", id);
                throw;
            }
        }
    }
}
