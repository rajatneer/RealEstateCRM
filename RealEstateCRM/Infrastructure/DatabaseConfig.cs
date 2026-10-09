namespace RealEstateCRM.Infrastructure
{
    public static class DatabaseConfig
    {
        /// <summary>
        /// DATABASE_URL (as set by Render/Heroku, postgres://...) wins and selects PostgreSQL.
        /// Otherwise Database:Provider ("Sqlite" or "Postgres") and ConnectionStrings:DefaultConnection are used.
        /// </summary>
        public static (string Provider, string ConnectionString) Resolve(IConfiguration config)
        {
            var url = config["DATABASE_URL"];
            if (!string.IsNullOrWhiteSpace(url) &&
                (url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                 url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
            {
                return ("Postgres", ToNpgsqlConnectionString(url));
            }

            var provider = config["Database:Provider"] ?? "Sqlite";
            var connectionString = config.GetConnectionString("DefaultConnection") ?? "Data Source=realestate_crm.db";
            return (provider, connectionString);
        }

        public static bool IsPostgres(string provider) =>
            provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase);

        public static string ToNpgsqlConnectionString(string url)
        {
            var uri = new Uri(url);
            var userInfo = uri.UserInfo.Split(':', 2);
            var user = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            return $"Host={uri.Host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";
        }
    }
}
