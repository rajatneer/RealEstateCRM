using System.ComponentModel.DataAnnotations;

namespace RealEstateCRM.Models.DTOs
{
    public class StageSaveDto
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Owner { get; set; }

        [Range(0, 3650)]
        public int DefaultDays { get; set; }

        [EnumDataType(typeof(MilestoneCategory))]
        public MilestoneCategory Category { get; set; } = MilestoneCategory.Other;
    }

    public class StageDto
    {
        public int Id { get; set; }
        public int Sequence { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Owner { get; set; }
        public int DefaultDays { get; set; }
        public MilestoneCategory Category { get; set; }
    }

    public static class StageMappings
    {
        public static StageDto ToDto(this StageTemplate s) => new()
        {
            Id = s.Id, Sequence = s.Sequence, Name = s.Name, Owner = s.Owner,
            DefaultDays = s.DefaultDays, Category = s.Category
        };
    }
}
