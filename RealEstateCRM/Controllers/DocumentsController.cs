using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Models;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    /// <summary>Paper-work files on a project. Anyone in the company can upload, view and download; only Owners can delete.</summary>
    [ApiController]
    [Route("api")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documents;

        public DocumentsController(IDocumentService documents) => _documents = documents;

        [HttpGet("projects/{projectId:int}/documents")]
        public async Task<IActionResult> List(int projectId) => Ok(await _documents.ListAsync(projectId));

        [HttpPost("projects/{projectId:int}/documents")]
        [RequestSizeLimit(DocumentService.MaxBytes + 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(int projectId, IFormFile file, [FromForm] int? milestoneId, [FromForm] string? note)
        {
            var by = User.FindFirst("name")?.Value;
            var dto = await _documents.UploadAsync(projectId, file, milestoneId, note, by);
            return StatusCode(StatusCodes.Status201Created, dto);
        }

        [HttpGet("documents/{id:int}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var doc = await _documents.GetAsync(id);
            if (doc == null) return NotFound();
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(doc.Data, doc.ContentType, doc.FileName);
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpDelete("documents/{id:int}")]
        public async Task<IActionResult> Delete(int id) =>
            await _documents.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
