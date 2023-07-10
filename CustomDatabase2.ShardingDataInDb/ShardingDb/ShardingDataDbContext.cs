// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.CommonCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CustomDatabase2.ShardingDataInDb.ShardingDb
{
    public class ShardingDataDbContext : DbContext
    {
        /// <summary>
        /// This holds the settings for the default <see cref="DatabaseInformation"/> entry
        /// </summary>
        private readonly DatabaseInformationOptions _setup;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="setup"></param>
        public ShardingDataDbContext(DbContextOptions<ShardingDataDbContext> options,
            DatabaseInformationOptions setup)
            : base(options)
        {
            _setup = setup;
        }

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

            //This is NOT added if the application's tenants are all in separate databases, i.e. sharding
            if (!_setup.AddIfEmpty) return;

            //This provides the default entry
            if (_setup.DatabaseType.IsNullOrEmpty())
                throw new AuthPermissionsBadDataException(
                    "You are using custom database, so you set the DatabaseType to the short " +
                    "form of the database provider name, e.g. SqlServer.");

            modelBuilder.Entity<DatabaseInformation>().HasData(new DatabaseInformation
            {
                Name = _setup.Name,
                DatabaseName = _setup.DatabaseName,
                ConnectionName = _setup.ConnectionName,
                DatabaseType = _setup.DatabaseType
            });
        }
    }
}