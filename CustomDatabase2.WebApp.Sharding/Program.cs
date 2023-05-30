// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using AuthPermissions.SupportCode.AddUsersServices;
using CustomDatabase2.InvoiceCode.Sharding.AppStart;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;
using CustomDatabase2.InvoiceCode.Sharding.Services;
using CustomDatabase2.SqliteCustomParts.Sharding;
using CustomDatabase2.WebApp.Sharding.Data;
using CustomDatabase2.WebApp.Sharding.PermissionsCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RunMethodsSequentially;

var builder = WebApplication.CreateBuilder(args);

#region change individual user accounts to Sqlite
//Sqlite needs a extra service to add the directory where Sqlite should be created
var combineDir = new SqliteCombineDirAndDbName(builder.Environment.WebRootPath);
var connectionString =
    combineDir.AddDirectoryToConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
//We need to register this as the sharding needs this to form correct Sqlite Tenant databases 
builder.Services.AddSingleton(combineDir);

//You need to create a migration for the individual user accounts DbContext
//add-migration CreateIdentitySchema -Context ApplicationDbContext -OutputDir Data\Migrations
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

#endregion

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();


builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.RegisterAuthPermissions<CustomDatabase2Permissions>(options =>
{
    options.TenantType = TenantTypes.SingleLevel;
    options.EncryptionKey = builder.Configuration[nameof(AuthPermissionsOptions.EncryptionKey)];
    options.PathToFolderToLock = builder.Environment.WebRootPath;
    options.SecondPartOfShardingFile = builder.Environment.EnvironmentName;
    options.Configuration = builder.Configuration;
})
    //NOTE: This uses the same database as the individual accounts DB
    .SetupMultiTenantShardingWithSqlite(connectionString, combineDir, new SqliteDbConfig())
    .IndividualAccountsAuthentication()
    .RegisterAddClaimToUser<AddTenantNameClaim>()
    .RegisterTenantChangeService<ShardingTenantChangeService>()
    .AddRolesPermissionsIfEmpty(CustomDatabase2AuthSetupData.RolesDefinition)
    .AddAuthUsersIfEmpty(CustomDatabase2AuthSetupData.UsersRolesDefinition)
    .RegisterFindUserInfoService<IndividualAccountUserLookup>()
    .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
    .AddSuperUserToIndividualAccounts()
    .SetupAspNetCoreAndDatabase(options =>
    {
        //Migrate individual account database
        options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<ApplicationDbContext>>();
        //Add demo users to the database (if no individual account exist)
        options.RegisterServiceToRunInJob<StartupServicesIndividualAccountsAddDemoUsers>();
    });

//This registers the AuthP's "Sign up for a new tenant, with versioning" feature
//for more details see https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki/Sign-up-for-a-new-tenant%2C-with-versioning
builder.Services.AddTransient<IAddNewUserManager, IndividualUserAddUserManager<IdentityUser>>();
builder.Services.AddTransient<ISignInAndCreateTenant, SignInAndCreateTenant>();
//If Sharding is turned on then include the following registration
builder.Services.AddTransient<IGetDatabaseForNewTenant, AddNewDbForNewTenantSqlite>();

builder.Services.RegisterInvoiceServicesSharding(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
