using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RealEstateCRM.Tenancy;

namespace RealEstateCRM.Models
{
    /// <summary>
    /// One standard stage of a villa project, defined once on the Stages page.
    /// New projects copy all stages in Sequence order, with the owner and default time set here.
    /// </summary>
    public class StageTemplate : ITenantEntity
    {
        [Key]
        public int Id { get; set; }

        [JsonIgnore]
        public int CompanyId { get; set; }

        public int Sequence { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Owner { get; set; }

        /// <summary>Days allowed for this stage. A project's deadlines are the running total from its start date.</summary>
        public int DefaultDays { get; set; }

        public MilestoneCategory Category { get; set; } = MilestoneCategory.Other;
    }
}
