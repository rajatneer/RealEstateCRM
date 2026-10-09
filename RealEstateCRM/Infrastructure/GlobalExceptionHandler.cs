using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RealEstateCRM.Infrastructure
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetails;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
        {
            _logger = logger;
            _problemDetails = problemDetails;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int status;
            string title;
            string detail;

            switch (exception)
            {
                case AppException app:
                    status = app.StatusCode;
                    title = status == StatusCodes.Status404NotFound ? "Not found"
                          : status == StatusCodes.Status409Conflict ? "Conflict"
                          : "Bad request";
                    detail = app.Message;
                    _logger.LogWarning("Request failed with {Status}: {Message}", status, app.Message);
                    break;

                case DbUpdateException dbEx:
                    status = StatusCodes.Status409Conflict;
                    title = "Conflict";
                    detail = "The change conflicts with existing data.";
                    _logger.LogError(dbEx, "Database update failed");
                    break;

                default:
                    status = StatusCodes.Status500InternalServerError;
                    title = "Server error";
                    detail = "An unexpected error occurred.";
                    _logger.LogError(exception, "Unhandled exception");
                    break;
            }

            httpContext.Response.StatusCode = status;
            return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
            });
        }
    }
}
