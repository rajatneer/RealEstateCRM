using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateCRM.Models
{
    public class Property
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? ZipCode { get; set; }

        [Required]
        public PropertyType Type { get; set; }

        [Required]
        public PropertyStatus Status { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Bedrooms { get; set; }

        public int Bathrooms { get; set; }

        public double SquareFeet { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? OwnerId { get; set; }
        public Contact? Owner { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
    }

    public enum PropertyType
    {
        House,
        Apartment,
        Condo,
        Townhouse,
        Land,
        Commercial
    }

    public enum PropertyStatus
    {
        Available,
        UnderContract,
        Sold,
        Rented,
        OffMarket
    }
}
