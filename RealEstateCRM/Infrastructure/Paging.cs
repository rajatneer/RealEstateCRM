using Microsoft.EntityFrameworkCore;

namespace RealEstateCRM.Infrastructure
{
    /// <summary>Reads ?page= and ?pageSize= from the request and reports the total via the X-Total-Count header.</summary>
    public interface IPagingContext
    {
        int Page { get; }
        int PageSize { get; }
        void SetTotal(int total);
    }

    public class HttpPagingContext : IPagingContext
    {
        public const int DefaultPageSize = 500;
        public const int MaxPageSize = 1000;

        private readonly IHttpContextAccessor _accessor;

        public HttpPagingContext(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public int Page => Read("page", 1, 1, 1_000_000);
        public int PageSize => Read("pageSize", DefaultPageSize, 1, MaxPageSize);

        public void SetTotal(int total)
        {
            var ctx = _accessor.HttpContext;
            if (ctx != null && !ctx.Response.HasStarted)
                ctx.Response.Headers["X-Total-Count"] = total.ToString();
        }

        private int Read(string key, int fallback, int min, int max)
        {
            var raw = _accessor.HttpContext?.Request.Query[key].ToString();
            return int.TryParse(raw, out var n) ? Math.Clamp(n, min, max) : fallback;
        }
    }

    public static class QueryExtensions
    {
        public static async Task<List<T>> ToPagedListAsync<T>(this IQueryable<T> query, IPagingContext paging, CancellationToken ct = default)
        {
            paging.SetTotal(await query.CountAsync(ct));
            return await query
                .Skip((paging.Page - 1) * paging.PageSize)
                .Take(paging.PageSize)
                .ToListAsync(ct);
        }
    }
}
