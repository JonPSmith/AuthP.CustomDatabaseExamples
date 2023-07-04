// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using TestSupport.Attributes;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestShardingSingleDbContext
{
    [Fact]
    public void TestShardingInvoiceDb()
    {
        //SETUP
        var testConnectionString = this.GetUniqueDatabaseConnectionString();
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlServer(this.GetUniqueDatabaseConnectionString(), dbOptions =>
            {
                dbOptions.MigrationsHistoryTable("__ShardingInvoiceMigrationsHistoryTable");
                dbOptions.MigrationsAssembly("CustomDatabase2.InvoiceCode.Sharding");
            }).Options;
        var context = new ShardingSingleDbContext(options, new ManualAddConnectionStringToDb(testConnectionString));
        context.Database.EnsureDeleted();

        //ATTEMPT
        context.Database.Migrate();
        context.Add(new CompanyTenant { CompanyName = "Test" });
        context.SaveChanges();

        //VERIFY
        context.Companies.Single().CompanyName.ShouldEqual("Test");
    }
}