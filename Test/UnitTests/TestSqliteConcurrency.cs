using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestSqliteConcurrency
{
    private readonly ITestOutputHelper _output;

    public TestSqliteConcurrency(ITestOutputHelper output)
    {
        _output = output;
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Version { get; set; }
    }

    public class OtherCustomer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TestConcurrencyDbContext : DbContext
    {
        public TestConcurrencyDbContext(DbContextOptions<TestConcurrencyDbContext> options)
            : base(options) {}

        public DbSet<Customer> Customers { get; set; }

        public DbSet<OtherCustomer> OtherCustomers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Customer>()
                .Property(c => c.Version)
                .HasDefaultValue(0)
                .IsRowVersion();

            var entityType = modelBuilder.Model.GetEntityTypes()
                .Single(x => x.Name.EndsWith("OtherCustomer"));
            entityType.AddProperty("Version", typeof(int));
            entityType.FindProperty("Version")
                .ValueGenerated = ValueGenerated.OnAddOrUpdate;
            entityType.FindProperty("Version")
                .SetDefaultValue(0);
            entityType.FindProperty("Version")
                .IsConcurrencyToken = true;
        }
    }

    [Fact]
    public void TestSqliteAuthPermissionsDbContext_ConcurrencyTokens_Customers()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptionsWithLogTo<TestConcurrencyDbContext>(_output.WriteLine);
        var context = new TestConcurrencyDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.Database.ExecuteSqlRaw(@"CREATE TRIGGER UpdateCustomerVersion
AFTER UPDATE ON Customers
BEGIN
    UPDATE Customers
    SET Version = Version + 1
    WHERE rowid = NEW.rowid;
END;
");

        var initial = new Customer{Name = "Initial"};
        context.Add(initial);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        //ATTEMPT
        var entity = context.Customers.Single();
        context.Database.ExecuteSqlInterpolated($"UPDATE Customers SET Name = 'XYZ'");
        entity.Name = "ABC";
        try
        {
            entity.Name = "ABC";
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
    public void TestSqliteAuthPermissionsDbContext_ConcurrencyTokens_OtherCustomers()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptionsWithLogTo<TestConcurrencyDbContext>(_output.WriteLine);
        var context = new TestConcurrencyDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.Database.ExecuteSqlRaw(@"CREATE TRIGGER UpdateOtherCustomerVersion
AFTER UPDATE ON OtherCustomers
BEGIN
    UPDATE OtherCustomers
    SET Version = Version + 1
    WHERE rowid = NEW.rowid;
END;
");

        var initial = new OtherCustomer { Name = "Initial" };
        context.Add(initial);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        //ATTEMPT
        var entity = context.OtherCustomers.Single();
        context.Database.ExecuteSqlInterpolated($"UPDATE OtherCustomers SET Name = 'XYZ'");
        entity.Name = "ABC";
        try
        {
            entity.Name = "ABC";
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


}