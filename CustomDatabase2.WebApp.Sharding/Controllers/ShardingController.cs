using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.ShardingServices;
using CustomDatabase2.WebApp.Sharding.Models;
using CustomDatabase2.WebApp.Sharding.PermissionsCode;
using Microsoft.AspNetCore.Mvc;

namespace CustomDatabase2.WebApp.Sharding.Controllers;

public class ShardingController : Controller
{
    private readonly IAccessDatabaseInformationVer5 _dbInfoService;

    public ShardingController(IAccessDatabaseInformationVer5 dbInfoService)
    {
        _dbInfoService = dbInfoService;
    }

    [HasPermission(CustomDatabase2Permissions.ListDatabaseInfos)]
    public IActionResult Index(string message)
    {
        ViewBag.Message = message;

        return View(_dbInfoService.ReadAllShardingInformation());
    }

    [HasPermission(CustomDatabase2Permissions.TenantCreate)]
    public IActionResult Create([FromServices] IShardingConnections service)
    {
        var dto = new DatabaseInformationEdit
        {
            AllPossibleConnectionNames = service.GetConnectionStringNames(),
            PossibleDatabaseTypes = service.ShardingDatabaseProviders.Keys.ToArray()
        };

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(CustomDatabase2Permissions.TenantCreate)]
    public IActionResult Create(DatabaseInformationEdit data)
    {
        var status = _dbInfoService.AddDatabaseInfoToShardingInformation(data.DatabaseInfo);

        if (status.HasErrors)
            return RedirectToAction(nameof(ErrorDisplay),
                new { errorMessage = status.GetAllErrors() });

        return RedirectToAction(nameof(Index), new { message = status.Message });
    }

    [HasPermission(CustomDatabase2Permissions.UpdateDatabaseInfo)]
    public ActionResult Edit([FromServices] IShardingConnections service, string name)
    {
        var dto = new DatabaseInformationEdit
        {
            DatabaseInfo = _dbInfoService.GetDatabaseInformationByName(name),
            AllPossibleConnectionNames = service.GetConnectionStringNames(),
            PossibleDatabaseTypes = service.ShardingDatabaseProviders.Keys.ToArray()
        };

        if (dto.DatabaseInfo == null)
            return RedirectToAction(nameof(ErrorDisplay),
                new { errorMessage = $"Could not find a database information with the name {name}." });

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(CustomDatabase2Permissions.AddDatabaseInfo)]
    public ActionResult Edit(DatabaseInformationEdit data)
    {
        var status = _dbInfoService.UpdateDatabaseInfoToShardingInformation(data.DatabaseInfo);

        if (status.HasErrors)
            return RedirectToAction(nameof(ErrorDisplay),
                new { errorMessage = status.GetAllErrors() });

        return RedirectToAction(nameof(Index), new { message = status.Message });
    }

    [HasPermission(CustomDatabase2Permissions.RemoveDatabaseInfo)]
    public IActionResult Remove(string name)
    {
        if (_dbInfoService.GetDatabaseInformationByName(name) == null)
            return RedirectToAction(nameof(ErrorDisplay),
                new { errorMessage = "Could not find that database information." });

        return View((object)name);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(CustomDatabase2Permissions.RemoveDatabaseInfo)]
    public async Task<IActionResult> Remove(string nameToRemove, bool dummyValue)
    {
        var status = await _dbInfoService.RemoveDatabaseInfoFromShardingInformationAsync(nameToRemove);

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