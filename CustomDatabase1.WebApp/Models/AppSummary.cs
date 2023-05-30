// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace CustomDatabase1.WebApp.Models
{
    public class AppSummary
    {
        public string Application { get; } = "ASP.NET Core MVC";
        public string AuthorizationProvider { get; } = "ASP.NET Core's individual users account";
        public string CookieOrToken { get; } = "Cookie";
        public string MultiTenant { get; } = "Custom database, single level multi-tenant";
        public string[] Databases { get; } = new []
        {
            "One Sqlite database shared by:",
            "- ASP.NET Core Individual accounts database",
            "- AuthPermissions' database",
            "- multi-tenant invoice database"
        };

        public string Note { get; } = "Example of versioning your application";
    }
}