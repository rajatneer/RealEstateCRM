using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;

namespace RealEstateCRM.Services
{
    public interface IDocumentService
    {
        Task<List<DocumentDto>> ListAsync(int projectId);
        Task<DocumentDto> UploadAsync(int projectId, IFormFile file, int? milestoneId, string? note, string? uploadedBy);
        Task<ProjectDocument?> GetAsync(int id);
        Task<bool> DeleteAsync(int id);
    }

    public class DocumentService : IDocumentService
    {
        public const long MaxBytes = 10 * 1024 * 1024;

        // Only these types are accepted. Anything a browser could render as a page (html, svg, js) is excluded on purpose.
        private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".gif"] = "image/gif",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".csv"] = "text/csv",
            [".txt"] = "text/plain",
        };

        private readonly CrmDbContext _context;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(CrmDbContext context, ILogger<DocumentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DocumentDto>> ListAsync(int projectId)
        {
            await _context.EnsureExistsAsync<Project>(projectId, "Project");

            // Project() keeps the file bytes out of the query.
            var docs = await _context.ProjectDocuments
                .Where(d => d.ProjectId == projectId)
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new DocumentDto
                {
                    Id = d.Id, ProjectId = d.ProjectId, MilestoneId = d.MilestoneId, FileName = d.FileName,
                    ContentType = d.ContentType, SizeBytes = d.SizeBytes, Note = d.Note,
                    UploadedBy = d.UploadedBy, UploadedAt = d.UploadedAt
                })
                .ToListAsync();

            var titles = await _context.ProjectMilestones
                .Where(m => m.ProjectId == projectId)
                .Select(m => new { m.Id, m.Title })
                .ToDictionaryAsync(m => m.Id, m => m.Title);
            foreach (var d in docs)
                if (d.MilestoneId.HasValue && titles.TryGetValue(d.MilestoneId.Value, out var t)) d.StageName = t;
            return docs;
        }

        public async Task<DocumentDto> UploadAsync(int projectId, IFormFile file, int? milestoneId, string? note, string? uploadedBy)
        {
            await _context.EnsureExistsAsync<Project>(projectId, "Project");

            if (file == null || file.Length == 0) throw new BadRequestException("Choose a file to upload.");
            if (file.Length > MaxBytes) throw new BadRequestException("File is too large. The limit is 10 MB.");

            var name = Path.GetFileName(file.FileName ?? string.Empty).Trim();
            var ext = Path.GetExtension(name);
            if (name.Length == 0 || !Allowed.TryGetValue(ext, out var contentType))
                throw new BadRequestException("This file type is not allowed. Use PDF, images, Word, Excel, CSV or text files.");
            if (name.Length > 255) name = name[^255..];

            string? stageTitle = null;
            if (milestoneId.HasValue)
            {
                stageTitle = await _context.ProjectMilestones
                    .Where(m => m.Id == milestoneId.Value && m.ProjectId == projectId)
                    .Select(m => m.Title).FirstOrDefaultAsync();
                if (stageTitle == null) throw new BadRequestException("That stage does not belong to this project.");
            }

            using var ms = new MemoryStream((int)file.Length);
            await file.CopyToAsync(ms);

            var doc = new ProjectDocument
            {
                ProjectId = projectId,
                MilestoneId = milestoneId,
                FileName = name,
                ContentType = contentType,
                SizeBytes = file.Length,
                Data = ms.ToArray(),
                Note = string.IsNullOrWhiteSpace(note) ? null : (note.Trim().Length > 500 ? note.Trim()[..500] : note.Trim()),
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };
            _context.ProjectDocuments.Add(doc);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Document {DocId} uploaded to project {ProjectId} ({Bytes} bytes)", doc.Id, projectId, doc.SizeBytes);

            return new DocumentDto
            {
                Id = doc.Id, ProjectId = projectId, MilestoneId = milestoneId, StageName = stageTitle, FileName = doc.FileName,
                ContentType = doc.ContentType, SizeBytes = doc.SizeBytes, Note = doc.Note,
                UploadedBy = doc.UploadedBy, UploadedAt = doc.UploadedAt
            };
        }

        public Task<ProjectDocument?> GetAsync(int id) =>
            _context.ProjectDocuments.FirstOrDefaultAsync(d => d.Id == id);

        public async Task<bool> DeleteAsync(int id)
        {
            var doc = await _context.ProjectDocuments.FirstOrDefaultAsync(d => d.Id == id);
            if (doc == null) return false;
            _context.ProjectDocuments.Remove(doc);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
