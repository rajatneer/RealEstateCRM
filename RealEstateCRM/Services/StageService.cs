using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;

namespace RealEstateCRM.Services
{
    public interface IStageService
    {
        Task<List<StageTemplate>> GetAllAsync();
        Task<StageTemplate> CreateAsync(StageSaveDto dto);
        Task<StageTemplate?> UpdateAsync(int id, StageSaveDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> MoveAsync(int id, bool up);
        /// <summary>Loads the standard stage list, only when the company has no stages yet. Returns false if stages already exist.</summary>
        Task<bool> SeedDefaultsAsync();
    }

    public class StageService : IStageService
    {
        private static readonly (string Name, string Owner, int Days, MilestoneCategory Cat)[] Defaults =
        {
            ("Title verification", "Nakul", 7, MilestoneCategory.Registry),
            ("Document repository setup", "Ridhima", 3, MilestoneCategory.Other),
            ("Land sale deed", "Nakul", 14, MilestoneCategory.Registry),
            ("Land mutation", "Nakul", 30, MilestoneCategory.Registry),
            ("Villa booking", "Nakul", 7, MilestoneCategory.Agreement),
            ("Client agreement and sale deed", "Ridhima", 14, MilestoneCategory.Agreement),
            ("Client mutation if applicable", "Manjunath", 30, MilestoneCategory.Registry),
            ("Survey and architectural plans", "Manjunath", 21, MilestoneCategory.Other),
            ("3D renders", "Manjunath", 14, MilestoneCategory.Other),
            ("VR renders if required", "Manjunath", 7, MilestoneCategory.Other),
            ("TCP approval", "Nakul", 45, MilestoneCategory.Approval),
            ("Health Department preconstruction approval", "Nakul", 30, MilestoneCategory.Approval),
            ("Forest clearance if applicable", "Nakul", 30, MilestoneCategory.Approval),
            ("Panchayat construction licence", "Nakul", 30, MilestoneCategory.Approval),
            ("Temporary electricity connection", "Nakul", 15, MilestoneCategory.Other),
            ("Temporary water connection", "Nakul", 10, MilestoneCategory.Other),
            ("Align contractor", "Nakul", 7, MilestoneCategory.Other),
            ("Align supervisor", "Nakul", 5, MilestoneCategory.Other),
            ("Bhoomi Poojan", "Nakul", 3, MilestoneCategory.Other),
            ("Site mobilisation and setting out", "Manjunath", 7, MilestoneCategory.Construction),
            ("Excavation", "Contractor", 10, MilestoneCategory.Construction),
            ("Foundations and plinth", "Contractor", 30, MilestoneCategory.Construction),
            ("Ground floor RCC structure", "Contractor", 30, MilestoneCategory.Construction),
            ("Upper floor RCC structure", "Contractor", 30, MilestoneCategory.Construction),
            ("Roof slab and staircase", "Contractor", 21, MilestoneCategory.Construction),
            ("Masonry", "Contractor", 30, MilestoneCategory.Construction),
            ("Concealed plumbing and electrical", "Contractor", 21, MilestoneCategory.Construction),
            ("Plastering", "Contractor", 21, MilestoneCategory.Construction),
            ("Waterproofing and testing", "Contractor", 10, MilestoneCategory.Construction),
            ("Pool shell and waterproofing", "Contractor", 30, MilestoneCategory.Construction),
            ("Grey structure complete", "Contractor", 3, MilestoneCategory.Construction),
            ("Align interior designer", "Nakul", 7, MilestoneCategory.Other),
            ("Interior design finalisation", "Nakul", 21, MilestoneCategory.Other),
            ("Tiles selection", "Client", 7, MilestoneCategory.Other),
            ("Client presentation and approval", "Nakul", 7, MilestoneCategory.Other),
            ("Procurement and delivery plan", "Nakul", 14, MilestoneCategory.Other),
            ("Start interior work", "Interior Designer", 3, MilestoneCategory.Construction),
            ("Floor and wall tiling", "Contractor", 21, MilestoneCategory.Construction),
            ("Doors windows and glazing", "Contractor", 21, MilestoneCategory.Construction),
            ("Kitchen wardrobes and joinery", "Contractor", 30, MilestoneCategory.Construction),
            ("Painting and finishes", "Contractor", 21, MilestoneCategory.Construction),
            ("Sanitary electrical AC and equipment", "Contractor", 14, MilestoneCategory.Construction),
            ("Pool equipment and commissioning", "Contractor", 14, MilestoneCategory.Construction),
            ("External works and landscaping", "Contractor", 30, MilestoneCategory.Construction),
            ("Snagging and rectification", "Contractor", 14, MilestoneCategory.Construction),
            ("Finishing complete", "Contractor", 3, MilestoneCategory.Construction),
            ("TCP completion certificate", "Nakul", 30, MilestoneCategory.Approval),
            ("Health Department completion approval", "Nakul", 21, MilestoneCategory.Approval),
            ("Panchayat occupancy certificate", "Nakul", 30, MilestoneCategory.Approval),
            ("Permanent connections and transfers", "Nakul", 30, MilestoneCategory.Other),
            ("Villa delivery", "Contractor", 7, MilestoneCategory.Other),
            ("Villa shoot", "Video Editor", 5, MilestoneCategory.Other),
            ("Client testimonial", "Ridhima", 7, MilestoneCategory.Other),
        };

        private readonly CrmDbContext _context;

        public StageService(CrmDbContext context) => _context = context;

        public Task<List<StageTemplate>> GetAllAsync() =>
            _context.StageTemplates.OrderBy(s => s.Sequence).ThenBy(s => s.Id).ToListAsync();

        public async Task<StageTemplate> CreateAsync(StageSaveDto dto)
        {
            var max = await _context.StageTemplates.Select(s => (int?)s.Sequence).MaxAsync() ?? 0;
            var stage = new StageTemplate { Sequence = max + 1 };
            Apply(stage, dto);
            _context.StageTemplates.Add(stage);
            await _context.SaveChangesAsync();
            return stage;
        }

        public async Task<StageTemplate?> UpdateAsync(int id, StageSaveDto dto)
        {
            var stage = await _context.StageTemplates.FirstOrDefaultAsync(s => s.Id == id);
            if (stage == null) return null;
            Apply(stage, dto);
            await _context.SaveChangesAsync();
            return stage;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var stage = await _context.StageTemplates.FirstOrDefaultAsync(s => s.Id == id);
            if (stage == null) return false;
            _context.StageTemplates.Remove(stage);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MoveAsync(int id, bool up)
        {
            var all = await _context.StageTemplates.OrderBy(s => s.Sequence).ThenBy(s => s.Id).ToListAsync();
            var index = all.FindIndex(s => s.Id == id);
            if (index < 0) return false;
            var other = up ? index - 1 : index + 1;
            if (other < 0 || other >= all.Count) return true;

            (all[index], all[other]) = (all[other], all[index]);
            for (var i = 0; i < all.Count; i++) all[i].Sequence = i + 1;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SeedDefaultsAsync()
        {
            if (await _context.StageTemplates.AnyAsync()) return false;
            var seq = 1;
            foreach (var d in Defaults)
                _context.StageTemplates.Add(new StageTemplate
                {
                    Sequence = seq++, Name = d.Name, Owner = d.Owner, DefaultDays = d.Days, Category = d.Cat
                });
            await _context.SaveChangesAsync();
            return true;
        }

        private static void Apply(StageTemplate stage, StageSaveDto dto)
        {
            stage.Name = dto.Name.Trim();
            stage.Owner = string.IsNullOrWhiteSpace(dto.Owner) ? null : dto.Owner.Trim();
            stage.DefaultDays = dto.DefaultDays;
            stage.Category = dto.Category;
        }
    }
}
