// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CustomDatabase1.SqliteCustomParts.AuthPermissionsDbMigrate
{
    public class AuthPermissionsDbContextSqlite : IDesignTimeDbContextFactory<AuthPermissionsDbContext>
    {
        // The connection string must be valid, but the connection string isn’t used when adding a migration.
        private const string connectionString = "Data source=PrimaryDatabase.sqlite";

        public AuthPermissionsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder =
                new DbContextOptionsBuilder<AuthPermissionsDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new AuthPermissionsDbContext(optionsBuilder.Options, null, new SqliteDbConfig());
        }
    }
    /******************************************************************************
    * NOTES ON MIGRATION:
    *
    * The AuthPermissionsDbContext is stored in the CustomDatabase1.SqliteCustomParts project
    * 
    * see https://docs.microsoft.com/en-us/aspnet/core/data/ef-rp/migrations?tabs=visual-studio
    * 
    * Add the following NuGet libraries to this project
    * 1. "Microsoft.EntityFrameworkCore.Tools"
    * 2. "Microsoft.EntityFrameworkCore.Sqlite" (or another database provider)
    * 
    * 2. Using Package Manager Console commands
    * The steps are:
    * a) Make sure the PMC default project is CustomDatabase1.SqliteCustomParts
    * b) Set the ASP.NET Core project as the startup project
    * b) Use the PMC command (NOTE: The first param, Version5, is the name of the migration)
    *    Add-Migration Version5 -Context AuthPermissionsDbContext
    *    
    * If you want to start afresh then:
    * a) Delete the current database
    * b) Delete all the class in the Migration directory
    * c) follow the steps to add a migration
    ******************************************************************************/
}