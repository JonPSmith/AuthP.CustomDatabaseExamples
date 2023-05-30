// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;

/// <summary>
/// This is the <see cref="ITenantChangeService"/> service for a single-level tenant with Sharding turned on.
/// This is different to the non-sharding versions, as we have to create the the instance of the application's
/// DbContext because the connection string relies on the <see cref="Tenant.DatabaseInfoName"/> in the tenant -
/// see <see cref="GetShardingSingleDbContext"/> at the end of this class. This also allows the DataKey to be added
/// which removes the need for using the IgnoreQueryFilters method on any queries
/// </summary>
public class ShardingTenantChangeService : ITenantChangeService
{
    private readonly IShardingConnections _connections;
    private readonly ILogger _logger;

    /// <summary>
    /// This allows the tenantId of the deleted tenant to be returned.
    /// This is useful if you want to soft delete the data
    /// </summary>
    public int DeletedTenantId { get; private set; }

    public ShardingTenantChangeService(IShardingConnections connections, ILogger<ShardingTenantChangeService> logger)
    {
        _connections = connections;
        _logger = logger;
    }

    /// <summary>
    /// This creates a <see cref="CompanyTenant"/> in the given database
    /// </summary>
    /// <param name="tenant">The tenant data used to create a new tenant</param>
    /// <returns>Returns null if all OK, otherwise the create is rolled back and the return string is shown to the user</returns>
    public async Task<string> CreateNewTenantAsync(Tenant tenant)
    {
        var checkError = await CheckDatabaseAndPossibleMigrate(tenant);
        if (checkError != null)
            return checkError;

        using var context = GetShardingSingleDbContext(tenant.DatabaseInfoName);
        if (context == null)
            return $"There is no connection string with the name {tenant.DatabaseInfoName}.";

        if (tenant.HasOwnDb && context.Companies.IgnoreQueryFilters().Any())
            return
                $"The tenant's {nameof(Tenant.HasOwnDb)} property is true, but the database contains existing companies";

        //You need this fixes the the "database is locked" error
        //see https://stackoverflow.com/a/72575774/1434764
        SqliteConnection.ClearAllPools();

        var newCompanyTenant = new CompanyTenant
        {
            AuthPTenantId = tenant.TenantId,
            CompanyName = tenant.TenantFullName
        };
        context.Add(newCompanyTenant);
        context.SaveChanges();

        return null;
    }

    public async Task<string> SingleTenantUpdateNameAsync(Tenant tenant)
    {
        using var context = GetShardingSingleDbContext(tenant.DatabaseInfoName);
        if (context == null)
            return $"There is no connection string with the name {tenant.DatabaseInfoName}.";

        var companyTenant = await context.Companies
            .SingleOrDefaultAsync(x => x.AuthPTenantId == tenant.TenantId);
        if (companyTenant != null)
        {
            companyTenant.CompanyName = tenant.TenantFullName;
            await context.SaveChangesAsync();
        }

        return null;
    }

    public async Task<string> SingleTenantDeleteAsync(Tenant tenant)
    {
        using var context = GetShardingSingleDbContext(tenant.DatabaseInfoName);
        if (context == null)
            return $"There is no connection string with the name {tenant.DatabaseInfoName}.";

        //You need this fixes the the "process cannot access the file XXX because it is being used by another process" error
        //see https://github.com/dotnet/efcore/issues/26580#issuecomment-963938116
        SqliteConnection.ClearAllPools();

        var builder = new SqliteConnectionStringBuilder(context.Database.GetConnectionString());
        var filePathToDb = builder.DataSource;
        if (context.Database.GetService<IRelationalDatabaseCreator>().Exists())
            File.Delete(filePathToDb);

        return null;
    }

    public Task<string> HierarchicalTenantUpdateNameAsync(List<Tenant> tenantsToUpdate)
    {
        throw new NotImplementedException();
    }

    public Task<string> HierarchicalTenantDeleteAsync(List<Tenant> tenantsInOrder)
    {
        throw new NotImplementedException();
    }

    public Task<string> MoveHierarchicalTenantDataAsync(List<(string oldDataKey, Tenant tenantToMove)> tenantToUpdate)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// This method can be quite complicated. It has to
    /// 1. Copy the data from the previous database into the new database
    /// 2. Delete the old data
    /// These two steps have to be done within a transaction, so that a failure to delete the old data will roll back the copy.
    /// </summary>
    /// <param name="oldDatabaseInfoName"></param>
    /// <param name="oldDataKey"></param>
    /// <param name="updatedTenant"></param>
    /// <returns></returns>
    public Task<string> MoveToDifferentDatabaseAsync(string oldDatabaseInfoName, string oldDataKey, Tenant updatedTenant)
    {
        //This application only has sharding, so this 
        //NOTE: Because this application uses Sqlite, then the normal move code wouldn't work
        //That's because the normal move code has a transaction within another transaction, which Sqlite doesn't support
        //If you want move with Sqlite, then read the article which has a solution to this.
        // https://www.codeproject.com/Articles/1127310/Nested-Transactions-for-System-Data-SQLite-with-Sa
        throw new NotImplementedException("This application only handles one db per user, so no moving is needed.");
    }

    //--------------------------------------------------
    //private methods / classes

    /// <summary>
    /// This check is a database is there and creates one if it isn't
    /// NOTE: It creates a separate context so that can be disposed before
    /// another part wants to access the database
    /// </summary>
    /// <param name="tenant"></param>
    /// <returns></returns>
    private async Task<string> CheckDatabaseAndPossibleMigrate(Tenant tenant)
    {
        using var context = GetShardingSingleDbContext(tenant.DatabaseInfoName);
        if (context == null)
            return $"There is no connection string with the name {tenant.DatabaseInfoName}.";

        //This finds if a Sqlite database exists
        if (!context.Database.GetService<IRelationalDatabaseCreator>().Exists())
            await context.Database.MigrateAsync();
        else if (!await context.Database.GetService<IRelationalDatabaseCreator>().HasTablesAsync())
            //The database exists but needs migrating
            await context.Database.MigrateAsync();

        return null;
    }

    /// <summary>
    /// This create a <see cref="ShardingSingleDbContext"/> with the correct connection string
    /// </summary>
    /// <param name="databaseDataName"></param>
    /// <param name="addPooling">if true, then adds </param>
    /// <returns><see cref="ShardingSingleDbContext"/> or null if connectionName wasn't found in the appsetting file</returns>
    private ShardingSingleDbContext? GetShardingSingleDbContext(string databaseDataName,
        bool addPooling = true)
    {
        var connectionString = _connections.FormConnectionString(databaseDataName);
        if (connectionString == null)
            throw new AuthPermissionsException(
                "The provided database information didn't provide a valid connection string");

        if (addPooling)
            connectionString += "; Pooling=FALSE";

        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlite(connectionString, dbOptions =>
                {
                    dbOptions.MigrationsHistoryTable("__ShardingInvoiceMigrationsHistoryTable");
                    dbOptions.MigrationsAssembly("CustomDatabase2.InvoiceCode.Sharding");
                }).Options;

        return new ShardingSingleDbContext(options);
    }
}