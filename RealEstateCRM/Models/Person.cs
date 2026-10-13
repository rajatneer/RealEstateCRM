using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RealEstateCRM.Tenancy;

namespace RealEstateCRM.Models
{
    /// <summary>A person who can be picked as Project Manager or Escalation Owner (master list).</summary>
    public class Person : ITenantEntity
    {
        [Key]
        public int Id { get; set; }

        [JsonIgnore]
        public int CompanyId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
