// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using CustomDatabase1.InvoiceCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetCore.AutoRegisterDi;

namespace CustomDatabase1.InvoiceCode.AppStart
{
    public static class StartupExtensions
    {
        public const string InvoicesDbContextHistoryName = "__InvoicesDbContextMigrationHistories";

        public static void RegisterInvoiceServices(this IServiceCollection services, string connectionString)
        {
            //Register any services in this project
            services.RegisterAssemblyPublicNonGenericClasses()
                .Where(c => c.Name.EndsWith("Service"))  //optional
                .AsPublicImplementedInterfaces();

            //Register the retail database to the same database used for individual accounts and AuthP database
            services.AddDbContext<InvoicesDbContext>(options =>
                options.UseSqlite(connectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(InvoicesDbContextHistoryName)));
        }
    }
}