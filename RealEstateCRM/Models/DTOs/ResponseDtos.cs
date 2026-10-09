namespace RealEstateCRM.Models.DTOs
{
    // Shapes returned by the API. Database entities are never serialized directly,
    // so internal columns (CompanyId, AssignedUserId, ...) cannot leak.

    public class ContactDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public ContactType Type { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PropertyDto
    {
        public int Id { get; set; }
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
        public ContactDto? Owner { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class InteractionDto
    {
        public int Id { get; set; }
        public int ContactId { get; set; }
        public ContactDto? Contact { get; set; }
        public int? PropertyId { get; set; }
        public PropertyDto? Property { get; set; }
        public InteractionType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LeadDto
    {
        public int Id { get; set; }
        public int ContactId { get; set; }
        public ContactDto? Contact { get; set; }
        public int? PropertyId { get; set; }
        public PropertyDto? Property { get; set; }
        public LeadStage Stage { get; set; }
        public LeadSource Source { get; set; }
        public decimal? EstimatedValue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CrmTaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public TaskPriority Priority { get; set; }
        public CrmTaskStatus Status { get; set; }
        public int? ContactId { get; set; }
        public ContactDto? Contact { get; set; }
        public int? PropertyId { get; set; }
        public PropertyDto? Property { get; set; }
        public int? LeadId { get; set; }
        public LeadDto? Lead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BrokerageDto
    {
        public int Id { get; set; }
        public int? LeadId { get; set; }
        public LeadDto? Lead { get; set; }
        public int? PropertyId { get; set; }
        public PropertyDto? Property { get; set; }
        public decimal DealValue { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GstAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public string? SubBrokerName { get; set; }
        public decimal? SubBrokerSplitPercent { get; set; }
        public decimal? SubBrokerAmount { get; set; }
        public BrokeragePaymentStatus PaymentStatus { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SiteVisitDto
    {
        public int Id { get; set; }
        public int ContactId { get; set; }
        public ContactDto? Contact { get; set; }
        public int PropertyId { get; set; }
        public PropertyDto? Property { get; set; }
        public DateTime ScheduledDate { get; set; }
        public SiteVisitStatus Status { get; set; }
        public string? PickupLocation { get; set; }
        public string? Feedback { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AssignDto
    {
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
        public int UserId { get; set; }
    }

    public static class DtoMappings
    {
        public static ContactDto ToDto(this Contact c) => new()
        {
            Id = c.Id, FirstName = c.FirstName, LastName = c.LastName, Email = c.Email, Phone = c.Phone,
            Type = c.Type, Notes = c.Notes, CreatedAt = c.CreatedAt
        };

        public static PropertyDto ToDto(this Property p) => p.ToDto(includeOwner: true);

        public static PropertyDto ToDto(this Property p, bool includeOwner) => new()
        {
            Id = p.Id, Address = p.Address, City = p.City, State = p.State, ZipCode = p.ZipCode,
            Type = p.Type, Status = p.Status, Price = p.Price, Bedrooms = p.Bedrooms, Bathrooms = p.Bathrooms,
            SquareFeet = p.SquareFeet, Description = p.Description, OwnerId = p.OwnerId,
            Owner = includeOwner ? p.Owner?.ToDto() : null, CreatedAt = p.CreatedAt
        };

        public static InteractionDto ToDto(this Interaction i) => new()
        {
            Id = i.Id, ContactId = i.ContactId, Contact = i.Contact?.ToDto(), PropertyId = i.PropertyId,
            Property = i.Property?.ToDto(false), Type = i.Type, Description = i.Description, Date = i.Date,
            CreatedAt = i.CreatedAt
        };

        public static LeadDto ToDto(this Lead l) => l.ToDto(includeRelations: true);

        public static LeadDto ToDto(this Lead l, bool includeRelations) => new()
        {
            Id = l.Id, ContactId = l.ContactId,
            Contact = includeRelations ? l.Contact?.ToDto() : null,
            PropertyId = l.PropertyId,
            Property = includeRelations ? l.Property?.ToDto(false) : null,
            Stage = l.Stage, Source = l.Source, EstimatedValue = l.EstimatedValue, Notes = l.Notes,
            CreatedAt = l.CreatedAt, UpdatedAt = l.UpdatedAt
        };

        public static CrmTaskDto ToDto(this CrmTask t) => new()
        {
            Id = t.Id, Title = t.Title, Description = t.Description, DueDate = t.DueDate, Priority = t.Priority,
            Status = t.Status, ContactId = t.ContactId, Contact = t.Contact?.ToDto(), PropertyId = t.PropertyId,
            Property = t.Property?.ToDto(false), LeadId = t.LeadId, Lead = t.Lead?.ToDto(false), CreatedAt = t.CreatedAt
        };

        public static BrokerageDto ToDto(this Brokerage b) => new()
        {
            Id = b.Id, LeadId = b.LeadId, Lead = b.Lead?.ToDto(true), PropertyId = b.PropertyId,
            Property = b.Property?.ToDto(false), DealValue = b.DealValue, CommissionPercent = b.CommissionPercent,
            CommissionAmount = b.CommissionAmount, GstPercent = b.GstPercent, GstAmount = b.GstAmount,
            TotalPayable = b.TotalPayable, SubBrokerName = b.SubBrokerName,
            SubBrokerSplitPercent = b.SubBrokerSplitPercent, SubBrokerAmount = b.SubBrokerAmount,
            PaymentStatus = b.PaymentStatus, Notes = b.Notes, CreatedAt = b.CreatedAt
        };

        public static SiteVisitDto ToDto(this SiteVisit v) => new()
        {
            Id = v.Id, ContactId = v.ContactId, Contact = v.Contact?.ToDto(), PropertyId = v.PropertyId,
            Property = v.Property?.ToDto(false), ScheduledDate = v.ScheduledDate, Status = v.Status,
            PickupLocation = v.PickupLocation, Feedback = v.Feedback, Notes = v.Notes, CreatedAt = v.CreatedAt
        };
    }
}
