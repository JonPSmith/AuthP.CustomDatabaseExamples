using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.PermissionsCode;
using CustomDatabase1.InvoiceCode.AppStart;
using CustomDatabase1.InvoiceCode.Dtos;
using CustomDatabase1.InvoiceCode.EfCoreClasses;
using CustomDatabase1.InvoiceCode.EfCoreCode;
using CustomDatabase1.InvoiceCode.Services;
using CustomDatabase1.WebApp.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomDatabase1.WebApp.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly InvoicesDbContext _context;

        public InvoiceController(InvoicesDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string message)
        {
            ViewBag.Message = message;

            var listInvoices = User.HasPermission(CustomDatabase1Permissions.InvoiceRead)
                ? await InvoiceSummaryDto.SelectInvoices(_context.Invoices)
                    .OrderByDescending(x => x.DateCreated)
                    .ToListAsync()
                : null;
            return View(listInvoices);
        }

        [HasPermission(CustomDatabase1Permissions.InvoiceCreate)]
        public IActionResult CreateInvoice()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(CustomDatabase1Permissions.InvoiceCreate)]
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
