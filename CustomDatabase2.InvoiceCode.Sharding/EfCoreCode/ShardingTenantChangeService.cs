// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Data;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses;
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

        if (tenant.HasOwnDb && context.Companies.Any())
            return
                $"The tenant's {nameof(Tenant.HasOwnDb)} property is true, but the database contains existing companies";

        var newCompanyTenant = new CompanyTenant
        {
            AuthPTenantId = tenant.TenantId,
            CompanyName = tenant.TenantFullName
        };
        context.Add(newCompanyTenant);
        await context.SaveChangesAsync();

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

    /// <summary>
    /// Typically you would delete the database, but that depends on what SQL Server provider you use.
    /// In this case I can the database because it is on a local SqlServer server.
    /// </summary>
    /// <param name="tenant">The tenant data used to create this tenant</param>
    /// <returns>Returns null if all OK, otherwise the create is rolled back and the return string is shown to the user</returns>
    public async Task<string> SingleTenantDeleteAsync(Tenant tenant)
    {
        using var context = GetShardingSingleDbContext(tenant.DatabaseInfoName);
        if (context == null)
            return $"There is no connection string with the name {tenant.DatabaseInfoName}.";

        await context.Database.EnsureDeletedAsync();

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
    /// Because there is a database per tenant, then there is no need to move tenant data
    /// </summary>
    public Task<string> MoveToDifferentDatabaseAsync(string oldDatabaseInfoName, string oldDataKey, Tenant updatedTenant)
    {
        throw new NotImplementedException();
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
    private async Task<string?> CheckDatabaseAndPossibleMigrate(Tenant tenant)
    {
        using var context = GetShardingSingleDbContext(tenant.DatabaseInfoName);
        if (context == null)
            return $"There is no connection string with the name {tenant.DatabaseInfoName}.";

        //Thanks to https://stackoverflow.com/questions/33911316/entity-framework-core-how-to-check-if-database-exists
        //There are various options to detect if a database is there - this seems the clearest
        if (!await context.Database.CanConnectAsync() ||
            await context.Database.GetService<IRelationalDatabaseCreator>().HasTablesAsync())
        {
            //The database doesn't exist, or it hasn't been migrated
            await context.Database.MigrateAsync();
        }

        return null;
    }

    /// <summary>
    /// This create a <see cref="ShardingSingleDbContext"/> with the correct connection string
    /// </summary>
    /// <param name="databaseDataName"></param>
    /// <returns><see cref="ShardingSingleDbContext"/> or null if connectionName wasn't found in the appsetting file</returns>
    private ShardingSingleDbContext? GetShardingSingleDbContext(string databaseDataName)
    {
        var connectionString = _connections.FormConnectionString(databaseDataName);
        if (connectionString == null)
            throw new AuthPermissionsException(
                "The provided database information didn't provide a valid connection string");

        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlServer(connectionString, dbOptions =>
            {
                dbOptions.MigrationsHistoryTable("__ShardingInvoiceMigrationsHistoryTable");
                dbOptions.MigrationsAssembly("CustomDatabase2.InvoiceCode.Sharding");
            }).Options;

        var shardingData = new ManualAddConnectionStringToDb(connectionString);
        return new ShardingSingleDbContext(options, shardingData);
    }
}