using System.ComponentModel.DataAnnotations;

namespace RealEstateCRM.Models.DTOs
{
    public class TimelineEntryDto
    {
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    // ── Contacts ──
    public class ContactCreateDto
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress, StringLength(200)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [EnumDataType(typeof(ContactType))]
        public ContactType Type { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class ContactUpdateDto : ContactCreateDto { }

    // ── Properties ──
    public class PropertyCreateDto
    {
        [Required, StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string State { get; set; } = string.Empty;

        [StringLength(10)]
        public string? ZipCode { get; set; }

        [EnumDataType(typeof(PropertyType))]
        public PropertyType Type { get; set; }

        [EnumDataType(typeof(PropertyStatus))]
        public PropertyStatus Status { get; set; }

        [Range(0, 10000000000000)]
        public decimal Price { get; set; }

        [Range(0, 100)]
        public int Bedrooms { get; set; }

        [Range(0, 100)]
        public int Bathrooms { get; set; }

        [Range(0, 10000000)]
        public double SquareFeet { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? OwnerId { get; set; }
    }

    public class PropertyUpdateDto : PropertyCreateDto { }

    // ── Interactions ──
    public class InteractionCreateDto
    {
        [Range(1, int.MaxValue)]
        public int ContactId { get; set; }

        public int? PropertyId { get; set; }

        [EnumDataType(typeof(InteractionType))]
        public InteractionType Type { get; set; }

        [Required, StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public DateTime? Date { get; set; }
    }

    // ── Leads ──
    public class LeadCreateDto
    {
        [Range(1, int.MaxValue)]
        public int ContactId { get; set; }

        public int? PropertyId { get; set; }

        [EnumDataType(typeof(LeadStage))]
        public LeadStage Stage { get; set; } = LeadStage.New;

        [EnumDataType(typeof(LeadSource))]
        public LeadSource Source { get; set; }

        [Range(0, 10000000000000)]
        public decimal? EstimatedValue { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class LeadUpdateDto : LeadCreateDto { }

    // ── Tasks ──
    public class CrmTaskCreateDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime DueDate { get; set; }

        [EnumDataType(typeof(TaskPriority))]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public int? ContactId { get; set; }
        public int? PropertyId { get; set; }
        public int? LeadId { get; set; }
    }

    public class CrmTaskUpdateDto : CrmTaskCreateDto
    {
        [EnumDataType(typeof(CrmTaskStatus))]
        public CrmTaskStatus Status { get; set; }
    }

    // ── Brokerage ──
    public class BrokerageCreateDto
    {
        public int? LeadId { get; set; }
        public int? PropertyId { get; set; }

        [Range(0.01, 10000000000000)]
        public decimal DealValue { get; set; }

        [Range(0, 100)]
        public decimal CommissionPercent { get; set; }

        [StringLength(200)]
        public string? SubBrokerName { get; set; }

        [Range(0, 100)]
        public decimal? SubBrokerSplitPercent { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class BrokerageUpdateDto : BrokerageCreateDto
    {
        [EnumDataType(typeof(BrokeragePaymentStatus))]
        public BrokeragePaymentStatus PaymentStatus { get; set; }
    }

    // ── Site visits ──
    public class SiteVisitCreateDto
    {
        [Range(1, int.MaxValue)]
        public int ContactId { get; set; }

        [Range(1, int.MaxValue)]
        public int PropertyId { get; set; }

        public DateTime ScheduledDate { get; set; }

        [StringLength(200)]
        public string? PickupLocation { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class SiteVisitUpdateDto : SiteVisitCreateDto
    {
        [EnumDataType(typeof(SiteVisitStatus))]
        public SiteVisitStatus Status { get; set; }

        [StringLength(500)]
        public string? Feedback { get; set; }
    }

    // ── Calculators ──
    public class EmiCalculatorRequest
    {
        [Range(1, 10000000000000)]
        public decimal LoanAmount { get; set; }

        [Range(0, 100)]
        public decimal AnnualInterestRate { get; set; }

        [Range(1, 600)]
        public int TenureMonths { get; set; }
    }

    public class EmiCalculatorResult
    {
        public decimal LoanAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TenureMonths { get; set; }
        public decimal MonthlyEmi { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalPayment { get; set; }
    }

    public class StampDutyRequest
    {
        [Range(0.01, 10000000000000)]
        public decimal PropertyValue { get; set; }

        [Required, StringLength(50)]
        public string State { get; set; } = string.Empty;
    }

    public class StampDutyResult
    {
        public decimal PropertyValue { get; set; }
        public string State { get; set; } = string.Empty;
        public decimal StampDutyPercent { get; set; }
        public decimal StampDutyAmount { get; set; }
        public decimal RegistrationPercent { get; set; }
        public decimal RegistrationAmount { get; set; }
        public decimal TotalCost { get; set; }
    }
}
