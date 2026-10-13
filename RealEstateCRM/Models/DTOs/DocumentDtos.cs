namespace RealEstateCRM.Models.DTOs
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? MilestoneId { get; set; }
        public string? StageName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string? Note { get; set; }
        public string? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
