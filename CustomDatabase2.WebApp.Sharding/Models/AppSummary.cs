// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace CustomDatabase2.WebApp.Sharding.Models
{
    public class AppSummary
    {
        public string Application { get; } = "ASP.NET Core MVC";
        public string AuthorizationProvider { get; } = "ASP.NET Core's individual users account";
        public string CookieOrToken { get; } = "Cookie";
        public string MultiTenant { get; } = "Custom database, with sharding";
        public string[] Databases { get; } = new []
        {
            "Individual accounts database (SqlServer)",
            "AuthPermissionsDbContext and ShardingDataDbContext (Postgres)",
            "Each Tenant database (SqlServer)"
        };

        public string Note { get; } = "Each Tenant has its own database, e.g. sharding";
    }
}