// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore;
using CustomDatabase2.InvoiceCode.Sharding.AppStart;
using CustomDatabase2.InvoiceCode.Sharding.Dtos;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;
using CustomDatabase2.InvoiceCode.Sharding.Services;
using CustomDatabase2.WebApp.Sharding.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomDatabase2.WebApp.Sharding.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly ShardingSingleDbContext _context;

        public InvoiceController(ShardingSingleDbContext context)
        {
            _context = context;
        }

        [HasPermission(CustomDatabase2Permissions.InvoiceRead)]
        public async Task<IActionResult> Index(string message)
        {
            ViewBag.Message = message;

            var listInvoices = await InvoiceSummaryDto.SelectInvoices(_context.Invoices)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync();
            return View(listInvoices);
        }

        [HasPermission(CustomDatabase2Permissions.InvoiceCreate)]
        public IActionResult CreateInvoice()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(CustomDatabase2Permissions.InvoiceCreate)]
        public async Task<IActionResult> CreateInvoice(Invoice invoice)
        {
            var builder = new ExampleInvoiceBuilder(null);
            var newInvoice = builder.CreateRandomInvoice(AddTenantNameClaim.GetTenantNameFromUser(User), invoice.InvoiceName);
            _context.Add(newInvoice);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { message = $"Added the invoice '{newInvoice.InvoiceName}'." });
        }
    }
}
