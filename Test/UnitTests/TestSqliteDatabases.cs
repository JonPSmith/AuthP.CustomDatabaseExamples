using System.Linq;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using CustomDatabase1.InvoiceCode.EfCoreClasses;
using CustomDatabase1.InvoiceCode.EfCoreCode;
using CustomDatabase1.SqliteCustomParts;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestSqliteDatabases
{
    private readonly ITestOutputHelper _output;

    public TestSqliteDatabases(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestSqliteInvoiceDb()
    {
        //SETUP
        var testDataPath = TestData.GetTestDataDir();
        var options = new DbContextOptionsBuilder<InvoicesDbContext>()
            .UseSqlite($"Data Source={testDataPath}\\invoice.sqlite")
            .Options;
        var context = new InvoicesDbContext(options, null);
        context.Database.EnsureDeleted();

        //ATTEMPT
        context.Database.Migrate();
        context.Add(new CompanyTenant { CompanyName = "Test" });
        context.SaveChanges();

        //VERIFY
        context.Companies.Single().CompanyName.ShouldEqual("Test");
    }

    [Fact]
    public void TestSqliteAuthPermissionsDbContext()
    {
        //SETUP
        var testDataPath = TestData.GetTestDataDir();
        var options = new DbContextOptionsBuilder<AuthPermissionsDbContext>()
            .UseSqlite($"Data Source={testDataPath}\\AuthPermissions.sqlite", dbOptions =>
                dbOptions.MigrationsAssembly("CustomDatabase1.SqliteCustomParts"))
            .Options;
        var context = new AuthPermissionsDbContext(options, null, new SqliteDbConfig());
        context.Database.EnsureDeleted();

        //ATTEMPT
        context.Database.Migrate();
        var refresh = RefreshToken.CreateNewRefreshToken("123", "xxxxx");
        context.Add(refresh);
        context.SaveChanges();

        //VERIFY
        context.RefreshTokens.Single().UserId.ShouldEqual("123");
    }

    [Fact]
    public void TestSqliteAuthPermissionsDbContext_ConcurrencyMessage()
    {
        //SETUP
        var testDataPath = TestData.GetTestDataDir();
        var builder = new DbContextOptionsBuilder<AuthPermissionsDbContext>()
            .EnableSensitiveDataLogging()
            .LogTo(_output.WriteLine, new[]{ RelationalEventId.CommandExecuting});
        EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(builder);
        builder.UseSqlite($"Data Source={testDataPath}\\ConcurrencyTokensDatabase.sqlite", dbOptions =>
        {
            dbOptions.MigrationsAssembly("CustomDatabase1.SqliteCustomParts");
        });
        var options = builder.Options;

        var context = new AuthPermissionsDbContext(options, null, new SqliteDbConfig());
        context.Database.EnsureDeleted();
        context.Database.Migrate(); //Have to use migration to get the trigger

        var initial = new RoleToPermissions("Test", null, "123");
        context.Add(initial);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        //ATTEMPT
        var entity = context.RoleToPermissions.Single();
        context.Database.ExecuteSqlInterpolated(
            $"UPDATE RoleToPermissions SET PackedPermissionsInRole = 'XYZ' WHERE RoleName = 'Test'");
        entity.Update("ABC");
        var status = context.SaveChangesWithChecks(new StubDefaultLocalizer());

        //VERIFY
        status.IsValid.ShouldBeFalse(status.GetAllErrors());
        status.GetAllErrors().ShouldEqual("Another user changed the RoleToPermissions with the name = Test. Please re-read the entity and add you change again.");
    }

    [Fact]
    public void TestSqliteAuthPermissionsDbContext_ConcurrencyException()
    {
        //SETUP
        var testDataPath = TestData.GetTestDataDir();
        var builder = new DbContextOptionsBuilder<AuthPermissionsDbContext>();
        builder.UseSqlite($"Data Source={testDataPath}\\ConcurrencyTokensDatabase.sqlite", dbOptions =>
        {
            dbOptions.MigrationsAssembly("CustomDatabase1.SqliteCustomParts");
        });
        var options = builder.Options;

        var context = new AuthPermissionsDbContext(options, null, new SqliteDbConfig());
        context.Database.EnsureDeleted();
        context.Database.Migrate(); //Have to use migration to get the trigger

        var initial = new RoleToPermissions("Test", null, "123");
        context.Add(initial);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        //ATTEMPT
        var entity = context.RoleToPermissions.Single();
        context.Database.ExecuteSqlInterpolated(
            $"UPDATE RoleToPermissions SET PackedPermissionsInRole = 'XYZ' WHERE RoleName = 'Test'");

        try
        {
            entity.Update("ABC");
            context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException e)
        {
            e.Message.ShouldStartWith("The database operation was expected to affect 1 row(s), but actually affected 0 row(s); data may have been modified or deleted since entities were loaded");
            return;
        }

        //VERIFY
        false.ShouldBeTrue("The concurrency event didn't trigger.");
    }


    [Fact]
    public void TestSqliteAuthPermissionsDbContext_Unique()
    {
        //SETUP
        var testDataPath = TestData.GetTestDataDir();
        var builder = new DbContextOptionsBuilder<AuthPermissionsDbContext>();
        EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(builder);
        builder.UseSqlite($"Data Source={testDataPath}\\ConcurrencyTokensDatabase.sqlite", dbOptions =>
        {
            dbOptions.MigrationsAssembly("CustomDatabase1.SqliteCustomParts");
        });
        var options = builder.Options;

        var context = new AuthPermissionsDbContext(options, null, new SqliteDbConfig());
        context.Database.EnsureDeleted();
        context.Database.Migrate();  //Have to use migration to get the trigger

        context.Add(new RoleToPermissions("BIG Name", null, "x"));
        context.SaveChanges();

        context.ChangeTracker.Clear();

        //ATTEMPT
        context.Add(new RoleToPermissions("BIG Name", null, "x"));
        var status = context.SaveChangesWithChecks(new StubDefaultLocalizer());

        //VERIFY
        status.IsValid.ShouldBeFalse();
        status.Errors.Count.ShouldEqual(1);
        status.Errors.Single().ToString().ShouldEqual("There is already a RoleToPermissions with a value: name = BIG Name");
    }
}