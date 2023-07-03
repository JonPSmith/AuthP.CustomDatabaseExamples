// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using TestSupport.Attributes;
using TestSupport.Helpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestShardingSingleDbContext
{
    [Fact]
    public void TestSqliteShardingInvoiceDb_RealDatabase()
    {
        //SETUP
        var testDataPath = TestData.GetTestDataDir();
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlite($"Data Source={testDataPath}\\shardingInvoice.sqlite", dbOptions =>
            {
                dbOptions.MigrationsHistoryTable("__ShardingInvoiceMigrationsHistoryTable");
                dbOptions.MigrationsAssembly("CustomDatabase2.InvoiceCode.Sharding");
            }).Options;
        var context = new ShardingSingleDbContext(options);
        context.Database.EnsureDeleted();

        //ATTEMPT
        context.Database.Migrate();
        context.Add(new CompanyTenant { CompanyName = "Test" });
        context.SaveChanges();

        //VERIFY
        context.Companies.Single().CompanyName.ShouldEqual("Test");
    }

    //[RunnableInDebugOnly]
    //public void TestSqliteShardingInvoiceDb_Tenant1()
    //{
    //    //SETUP
    //    var filePath =
    //        "C:\\Users\\JonPSmith\\source\\repos\\AuthPermissions.CustomDatabaseExamples\\CustomDatabase2.WebApp.Sharding\\wwwroot\\Tenant_1.sqlite";
    //    var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
    //        .UseSqlite($"Data Source={filePath}", dbOptions =>
    //        {
    //            dbOptions.MigrationsHistoryTable("__ShardingInvoiceMigrationsHistoryTable");
    //            dbOptions.MigrationsAssembly("CustomDatabase2.InvoiceCode.Sharding");
    //        }).Options;
    //    var context = new ShardingSingleDbContext(options);

    //    //ATTEMPT
    //    var company = context.Companies.Single();

    //    //VERIFY
    //    company.CompanyName.ShouldEqual("xxx");
    //}
}