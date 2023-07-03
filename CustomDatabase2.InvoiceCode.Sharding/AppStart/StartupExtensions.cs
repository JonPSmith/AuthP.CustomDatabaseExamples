// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using NetCore.AutoRegisterDi;

namespace CustomDatabase2.InvoiceCode.Sharding.AppStart
{
    public static class StartupExtensions
    {
        public const string ShardingSingleDbContextHistoryName = "__InvoicesDbContextMigrationHistories";

        public static void RegisterInvoiceServicesSharding(this IServiceCollection services, string connectionString)
        {
            //Register any services in this project
            services.RegisterAssemblyPublicNonGenericClasses()
                .Where(c => c.Name.EndsWith("Service"))  //optional
                .AsPublicImplementedInterfaces();

            //Register the retail database to the same database used for
            services.AddDbContext<ShardingSingleDbContext>(options =>
                options.UseSqlServer(connectionString, dbOptions =>
                {
                    dbOptions.MigrationsHistoryTable(ShardingSingleDbContextHistoryName);
                    dbOptions.MigrationsAssembly("CustomDatabase2.InvoiceCode.Sharding");
                }));
        }
    }
}