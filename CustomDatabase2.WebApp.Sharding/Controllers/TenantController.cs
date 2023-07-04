﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using CustomDatabase2.WebApp.Sharding.Models;
using CustomDatabase2.WebApp.Sharding.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomDatabase2.WebApp.Sharding.Controllers
{
    public class TenantController : Controller
    {
        private readonly IAuthTenantAdminService _authTenantAdmin;

        public TenantController(IAuthTenantAdminService authTenantAdmin)
        {
            _authTenantAdmin = authTenantAdmin;
        }

        [HasPermission(CustomDatabase2Permissions.TenantList)]
        public async Task<IActionResult> Index(string message)
        {
            var tenantNames = await ShardingSingleLevelTenantDto.TurnIntoDisplayFormat( _authTenantAdmin.QueryTenants())
                .OrderBy(x => x.TenantName)
                .ToListAsync();

            ViewBag.Message = message;

            return View(tenantNames);
        }

        [HasPermission(CustomDatabase2Permissions.ListDbsWithTenants)]
        public async Task<IActionResult> ListDatabases([FromServices] IShardingConnections connect)
        {
            var connections = await connect.GetDatabaseInfoNamesWithTenantNamesAsync();

            return View(connections);
        }

        [HasPermission(CustomDatabase2Permissions.TenantCreate)]
        public IActionResult Create([FromServices]AuthPermissionsOptions authOptions, 
        [FromServices]IShardingConnections connect)
        {
            return View(ShardingSingleLevelTenantDto.SetupForCreate(authOptions,
                connect.GetAllPossibleShardingData().Select(x => x.Name).ToList()
                ));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(CustomDatabase2Permissions.TenantCreate)]
        public async Task<IActionResult> Create(ShardingSingleLevelTenantDto input)
        {
            var status = await _authTenantAdmin.AddSingleTenantAsync(input.TenantName, null,
                input.HasOwnDb, input.ConnectionName);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }

        [HasPermission(CustomDatabase2Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(int id)
        {
            return View(await ShardingSingleLevelTenantDto.SetupForUpdateAsync(_authTenantAdmin, id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(CustomDatabase2Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(ShardingSingleLevelTenantDto input)
        {
            var status = await _authTenantAdmin
                .UpdateTenantNameAsync(input.TenantId, input.TenantName);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }


        [HasPermission(CustomDatabase2Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(int id)
        {
            var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(new ShardingSingleLevelTenantDto
            {
                TenantId = id,
                TenantName = status.Result.TenantFullName,
                DataKey = status.Result.GetTenantDataKey()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(CustomDatabase2Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(ShardingSingleLevelTenantDto input)
        {
            var status = await _authTenantAdmin.DeleteTenantAsync(input.TenantId);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object)errorMessage);
        }
    }
}
