// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Diagnostics;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.SupportCode.AddUsersServices;
using CustomDatabase2.InvoiceCode.Sharding.Services;
using CustomDatabase2.WebApp.Sharding.Models;
using CustomDatabase2.WebApp.Sharding.PermissionsCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CustomDatabase2.WebApp.Sharding.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(string message)
        {
            ViewBag.Message = message;

            if (AddTenantNameClaim.GetTenantNameFromUser(User) == null)
                return View(new AppSummary());

            return RedirectToAction("Index", "Invoice");
        }

        public IActionResult CreateTenant()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index",
                    new { message = "You can't create a new tenant because you are all ready logged in." });

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTenant([FromServices] ISignInAndCreateTenant userRegisterInvite,
            string tenantName, string email, string password, string version, bool isPersistent)
        {
            var newUserData = new AddNewUserDto { Email = email, Password = password, IsPersistent = isPersistent };
            var newTenantData = new AddNewTenantDto { TenantName = tenantName, Version = version };
            var status = await userRegisterInvite.SignUpNewTenantWithVersionAsync(newUserData, newTenantData,
                CreateTenantVersions.TenantSetupData);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index),
                new { message = status.Message });
        }

        [AllowAnonymous]
        public IActionResult CheckCreateNewShard(
            [FromServices] IAccessDatabaseInformationVer5 setSharding,
            [FromServices] IShardingConnections getSharding)
        {
            //Create the sharding information or this new
            var tenantRef = Guid.NewGuid().ToString();
            var databaseInfo = new DatabaseInformation
            {
                Name = tenantRef,
                ConnectionName = "DefaultConnection",
                DatabaseName = tenantRef,
                DatabaseType = "Sqlite"
            };

            var before = getSharding.GetAllPossibleShardingData().ToList();

            //This adds a new DatabaseInformation to the shardingsettings
            var status = setSharding.AddDatabaseInfoToShardingInformation(databaseInfo);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            var after = getSharding.GetAllPossibleShardingData().ToList();
            if (after.Count > before.Count)
                return RedirectToAction(nameof(Index),
                    new { message = "Success: the created sharding was found." });
            
            return RedirectToAction(nameof(ErrorDisplay),
                new { errorMessage = $"ERROR: Has {after.Count} shardings." });
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object)errorMessage);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
