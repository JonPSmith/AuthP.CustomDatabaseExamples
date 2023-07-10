// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CustomDatabase2.ShardingDataInDb.ShardingDb
{
    public class ShardingDbDesignTimeContextFactory : IDesignTimeDbContextFactory<ShardingDataDbContext>          
    {
        // This connection links to an invalidate database, but that's OK as I only used the Add-Migration command
        private const string connectionString = "host=127.0.0.1;Database=AuthP-Test";

        public ShardingDataDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder =
                new DbContextOptionsBuilder<ShardingDataDbContext>();
            optionsBuilder.UseNpgsql(connectionString, dbOptions =>
                dbOptions.MigrationsHistoryTable("__ShardingDataDbMigration"));


            return new ShardingDataDbContext(optionsBuilder.Options, new DatabaseInformationOptions(false));
        }
    }
    /******************************************************************************
    * NOTES ON MIGRATION:
    *
    * The ShardingDataDbContext is stored in the CustomDatabase2.ShardingDataInDb.ShardingDb project
    * 
    * see https://docs.microsoft.com/en-us/aspnet/core/data/ef-rp/migrations?tabs=visual-studio
    * 
    * Add the following NuGet libraries to this project
    * 1. "Microsoft.EntityFrameworkCore.Tools"
    * 2. "Npgsql.EntityFrameworkCore.PostgreSQL" (or another database provider)
    * 
    * 2. Using Package Manager Console commands
    * The steps are:
    * a) Make sure the default project is CustomDatabase2.ShardingDataInDb.ShardingDb
    * b) Set the CustomDatabase2.WebApp project as the startup project
    * b) Use the PMC command
    *    Add-Migration Initial -Context ShardingDataDbContext -OutputDir ShardingDb/Migrations
    * c) Don't migrate the database using the Update-database, but use the AddDatabaseOnStartup extension
    *    method when registering the AuthPermissions in ASP.NET Core.
    *    
    * If you want to start afresh then:
    * a) Delete the current database
    * b) Delete all the class in the Migration directory
    * c) follow the steps to add a migration
    ******************************************************************************/
}