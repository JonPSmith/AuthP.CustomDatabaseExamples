// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.BaseCode.CommonCode;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses;
using Microsoft.EntityFrameworkCore;

namespace CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;

/// <summary>
/// This is a DBContext that supports sharding only, i.e. each tenant has its own database
/// </summary>
public class ShardingSingleDbContext : DbContext
{
    /// <summary>
    /// This 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="shardingDataKeyAndConnect">This uses a service that obtains the DataKey and database connection string
    /// via the claims in the logged in users.</param>
    public ShardingSingleDbContext(DbContextOptions<ShardingSingleDbContext> options,
        IGetShardingDataFromUser shardingDataKeyAndConnect)
        : base(options)
    {
        if (shardingDataKeyAndConnect.ConnectionString == null)
            throw new AuthPermissionsException("You must be logged in as a tenant user.");

        Database.SetConnectionString(shardingDataKeyAndConnect.ConnectionString);
    }

    public DbSet<CompanyTenant> Companies { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<LineItem> LineItems { get; set; }

}