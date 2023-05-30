// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses;
using Microsoft.EntityFrameworkCore;

namespace CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;

/// <summary>
/// This is a DBContext that supports sharding 
/// </summary>
public class ShardingSingleDbContext : DbContext
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    public ShardingSingleDbContext(DbContextOptions<ShardingSingleDbContext> options)
        : base(options) { }

    public DbSet<CompanyTenant> Companies { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<LineItem> LineItems { get; set; }

}