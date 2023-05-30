// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using CustomDatabase1.InvoiceCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;

namespace CustomDatabase1.InvoiceCode.AppStart
{
    /// <summary>
    /// If there are no RetailOutlets in the RetailDbContext it seeds the RetailDbContext with RetailOutlets and gives each of them some stock
    /// </summary>

    public class StartupServiceSeedInvoiceDbContext : IStartupServiceToRunSequentially
    {
        public int OrderNum { get; } //runs after migration of the InvoicesDbContext

        public async ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
        {
            var context = scopedServices.GetRequiredService<InvoicesDbContext>();
            var numInvoices = await context.Invoices.IgnoreQueryFilters().CountAsync();
            if (numInvoices == 0)
            {
                var authTenantAdmin = scopedServices.GetRequiredService<IAuthTenantAdminService>();

                var seeder = new SeedInvoiceDbContext(context);
                await seeder.SeedInvoicesForAllTenantsAsync(authTenantAdmin.QueryTenants().ToArray());
            }
        }

    }
}