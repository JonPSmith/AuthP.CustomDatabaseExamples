// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using Microsoft.EntityFrameworkCore;

namespace CustomDatabase2.ShardingDataInDb.ShardingDb
{
    public class ShardingDataDbContext : DbContext
    {
        /// <summary>
        /// This defines the name of the connection string for the main database
        /// </summary>
        public string ShardingDefaultDatabaseInfoName { get; } = "Default Database";

        public ShardingDataDbContext(DbContextOptions<ShardingDataDbContext> options)
            : base(options) {}

        public DbSet<DatabaseInformation> ShardingData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DatabaseInformation>()
                .HasKey(x => x.Name);

            modelBuilder.Entity<DatabaseInformation>()
                .Property(x => x.Name).IsRequired();
            modelBuilder.Entity<DatabaseInformation>()
                .Property(x => x.ConnectionName).IsRequired();
            modelBuilder.Entity<DatabaseInformation>()
                .Property(x => x.DatabaseType).IsRequired();

            //This provides the default setting 
            modelBuilder.Entity<DatabaseInformation>().HasData(
                new DatabaseInformation
                {
                    Name = ShardingDefaultDatabaseInfoName,
                    DatabaseName = null,
                    ConnectionName = "DefaultConnection",
                    DatabaseType = this.GetProviderShortName()
                });
        }
    }
}