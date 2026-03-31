namespace RealEstateCRM.Models.DTOs
{
    public class TimelineEntryDto
    {
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
    public class ContactCreateDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public ContactType Type { get; set; }
        public string? Notes { get; set; }
    }

    public class ContactUpdateDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public ContactType Type { get; set; }
        public string? Notes { get; set; }
    }

    public class PropertyCreateDto
    {
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string? ZipCode { get; set; }
        public PropertyType Type { get; set; }
        public PropertyStatus Status { get; set; }
        public decimal Price { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public double SquareFeet { get; set; }
        public string? Description { get; set; }
        public int? OwnerId { get; set; }
    }

    public class PropertyUpdateDto
    {
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string? ZipCode { get; set; }
        public PropertyType Type { get; set; }
        public PropertyStatus Status { get; set; }
        public decimal Price { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public double SquareFeet { get; set; }
        public string? Description { get; set; }
        public int? OwnerId { get; set; }
    }

    public class InteractionCreateDto
    {
        public int ContactId { get; set; }
        public int? PropertyId { get; set; }
        public InteractionType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
    }

    // Lead DTOs
    public class LeadCreateDto
    {
        public int ContactId { get; set; }
        public int? PropertyId { get; set; }
        public LeadStage Stage { get; set; } = LeadStage.New;
        public LeadSource Source { get; set; }
        public decimal? EstimatedValue { get; set; }
        public string? Notes { get; set; }
    }

    public class LeadUpdateDto
    {
        public int ContactId { get; set; }
        public int? PropertyId { get; set; }
        public LeadStage Stage { get; set; }
        public LeadSource Source { get; set; }
        public decimal? EstimatedValue { get; set; }
        public string? Notes { get; set; }
    }

    // Task DTOs
    public class CrmTaskCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public int? ContactId { get; set; }
        public int? PropertyId { get; set; }
        public int? LeadId { get; set; }
    }

    public class CrmTaskUpdateDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public TaskPriority Priority { get; set; }
        public CrmTaskStatus Status { get; set; }
        public int? ContactId { get; set; }
        public int? PropertyId { get; set; }
        public int? LeadId { get; set; }
    }

    // Brokerage DTOs
    public class BrokerageCreateDto
    {
        public int? LeadId { get; set; }
        public int? PropertyId { get; set; }
        public decimal DealValue { get; set; }
        public decimal CommissionPercent { get; set; }
        public string? SubBrokerName { get; set; }
        public decimal? SubBrokerSplitPercent { get; set; }
        public string? Notes { get; set; }
    }

    public class BrokerageUpdateDto
    {
        public int? LeadId { get; set; }
        public int? PropertyId { get; set; }
        public decimal DealValue { get; set; }
        public decimal CommissionPercent { get; set; }
        public string? SubBrokerName { get; set; }
        public decimal? SubBrokerSplitPercent { get; set; }
        public BrokeragePaymentStatus PaymentStatus { get; set; }
        public string? Notes { get; set; }
    }

    // Site Visit DTOs
    public class SiteVisitCreateDto
    {
        public int ContactId { get; set; }
        public int PropertyId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string? PickupLocation { get; set; }
        public string? Notes { get; set; }
    }

    public class SiteVisitUpdateDto
    {
        public int ContactId { get; set; }
        public int PropertyId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public SiteVisitStatus Status { get; set; }
        public string? PickupLocation { get; set; }
        public string? Feedback { get; set; }
        public string? Notes { get; set; }
    }

    // EMI Calculator DTOs
    public class EmiCalculatorRequest
    {
        public decimal LoanAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
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
        public decimal PropertyValue { get; set; }
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
