using System.Text.Json.Serialization;
using RealEstateCRM.Tenancy;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateCRM.Models
{
    public class Brokerage : ITenantEntity
    {
        [Key]
        public int Id { get; set; }

        [JsonIgnore]
        public int CompanyId { get; set; }

        public int? LeadId { get; set; }
        public Lead? Lead { get; set; }

        public int? PropertyId { get; set; }
        public Property? Property { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DealValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal GstPercent { get; set; } = 18.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GstAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPayable { get; set; }

        [MaxLength(200)]
        public string? SubBrokerName { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? SubBrokerSplitPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SubBrokerAmount { get; set; }

        [Required]
        public BrokeragePaymentStatus PaymentStatus { get; set; } = BrokeragePaymentStatus.Pending;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum BrokeragePaymentStatus
    {
        Pending,
        PartiallyPaid,
        Paid
    }
}
