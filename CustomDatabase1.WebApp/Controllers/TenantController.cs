﻿using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.AccessTenantData;
using AuthPermissions.BaseCode.CommonCode;
using CustomDatabase1.WebApp.Models;
using CustomDatabase1.WebApp.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomDatabase1.WebApp.Controllers
{
    public class TenantController : Controller
    {
        private readonly IAuthTenantAdminService _authTenantAdmin;

        public TenantController(IAuthTenantAdminService authTenantAdmin)
        {
            _authTenantAdmin = authTenantAdmin;
        }

        [HasPermission(CustomDatabase1Permissions.TenantList)]
        public async Task<IActionResult> Index(string message)
        {
            var tenantNames = await SingleLevelTenantDto.TurnIntoDisplayFormat( _authTenantAdmin.QueryTenants())
                .OrderBy(x => x.TenantName)
                .ToListAsync();

            ViewBag.Message = message;

            return View(tenantNames);
        }

        [HasPermission(CustomDatabase1Permissions.TenantCreate)]
        public async Task<IActionResult> Create()
        {
            return View(new SingleLevelTenantDto { AllPossibleRoleNames = await _authTenantAdmin.GetRoleNamesForTenantsAsync() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(CustomDatabase1Permissions.TenantCreate)]
        public async Task<IActionResult> Create(SingleLevelTenantDto input)
        {
            var status = await _authTenantAdmin.AddSingleTenantAsync(input.TenantName, input.TenantRolesName);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }

        [HasPermission(CustomDatabase1Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(int id)
        {
            return View(await SingleLevelTenantDto.SetupForUpdateAsync(_authTenantAdmin, id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(CustomDatabase1Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(SingleLevelTenantDto input)
        {
            var status = await _authTenantAdmin
                .UpdateTenantNameAsync(input.TenantId, input.TenantName);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }


        [HasPermission(CustomDatabase1Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(int id)
        {
            var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(new SingleLevelTenantDto
            {
                TenantId = id,
                TenantName = status.Result.TenantFullName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(CustomDatabase1Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(SingleLevelTenantDto input)
        {
            var status = await _authTenantAdmin.DeleteTenantAsync(input.TenantId);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }

        [HasPermission(CustomDatabase1Permissions.TenantAccessData)]
        public async Task<IActionResult> StartAccess([FromServices] ILinkToTenantDataService service, int id)
        {
            var currentUser = User.GetUserIdFromUser();
            var status = await service.StartLinkingToTenantDataAsync(currentUser, id);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }

        public IActionResult StopAccess([FromServices] ILinkToTenantDataService service, bool gotoHome)
        {
            var currentUser = User.GetUserIdFromUser();
            service.StopLinkingToTenant();

            return gotoHome 
                ? RedirectToAction(nameof(Index), "Home") 
                : RedirectToAction(nameof(Index), new { message = "Finished linking to tenant's data" });
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object)errorMessage);
        }
    }
}
