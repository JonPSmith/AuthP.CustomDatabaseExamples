using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDatabase2.SqliteCustomParts.Sharding.Migrations
{
    public partial class Version5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "authp");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "authp",
                columns: table => new
                {
                    TokenValue = table.Column<string>(type: "TEXT", unicode: false, maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    JwtId = table.Column<string>(type: "TEXT", nullable: true),
                    IsInvalid = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddedDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", rowVersion: true, nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.TokenValue);
                });

            migrationBuilder.CreateTable(
                name: "RoleToPermissions",
                schema: "authp",
                columns: table => new
                {
                    RoleName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    RoleType = table.Column<byte>(type: "INTEGER", nullable: false),
                    PackedPermissionsInRole = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", rowVersion: true, nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleToPermissions", x => x.RoleName);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "authp",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentDataKey = table.Column<string>(type: "TEXT", unicode: false, maxLength: 250, nullable: true),
                    TenantFullName = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    IsHierarchical = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasOwnDb = table.Column<bool>(type: "INTEGER", nullable: false),
                    DatabaseInfoName = table.Column<string>(type: "TEXT", nullable: true),
                    ParentTenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", rowVersion: true, nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.TenantId);
                    table.ForeignKey(
                        name: "FK_Tenants_Tenants_ParentTenantId",
                        column: x => x.ParentTenantId,
                        principalSchema: "authp",
                        principalTable: "Tenants",
                        principalColumn: "TenantId");
                });

            migrationBuilder.CreateTable(
                name: "AuthUsers",
                schema: "authp",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    IsDisabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", rowVersion: true, nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthUsers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_AuthUsers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "authp",
                        principalTable: "Tenants",
                        principalColumn: "TenantId");
                });

            migrationBuilder.CreateTable(
                name: "RoleToPermissionsTenant",
                schema: "authp",
                columns: table => new
                {
                    TenantRolesRoleName = table.Column<string>(type: "TEXT", nullable: false),
                    TenantsTenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", rowVersion: true, nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleToPermissionsTenant", x => new { x.TenantRolesRoleName, x.TenantsTenantId });
                    table.ForeignKey(
                        name: "FK_RoleToPermissionsTenant_RoleToPermissions_TenantRolesRoleName",
                        column: x => x.TenantRolesRoleName,
                        principalSchema: "authp",
                        principalTable: "RoleToPermissions",
                        principalColumn: "RoleName",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleToPermissionsTenant_Tenants_TenantsTenantId",
                        column: x => x.TenantsTenantId,
                        principalSchema: "authp",
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserToRoles",
                schema: "authp",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    RoleName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", rowVersion: true, nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserToRoles", x => new { x.UserId, x.RoleName });
                    table.ForeignKey(
                        name: "FK_UserToRoles_AuthUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "authp",
                        principalTable: "AuthUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserToRoles_RoleToPermissions_RoleName",
                        column: x => x.RoleName,
                        principalSchema: "authp",
                        principalTable: "RoleToPermissions",
                        principalColumn: "RoleName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthUsers_Email",
                schema: "authp",
                table: "AuthUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthUsers_TenantId",
                schema: "authp",
                table: "AuthUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthUsers_UserName",
                schema: "authp",
                table: "AuthUsers",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AddedDateUtc",
                schema: "authp",
                table: "RefreshTokens",
                column: "AddedDateUtc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleToPermissions_RoleType",
                schema: "authp",
                table: "RoleToPermissions",
                column: "RoleType");

            migrationBuilder.CreateIndex(
                name: "IX_RoleToPermissionsTenant_TenantsTenantId",
                schema: "authp",
                table: "RoleToPermissionsTenant",
                column: "TenantsTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ParentDataKey",
                schema: "authp",
                table: "Tenants",
                column: "ParentDataKey");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ParentTenantId",
                schema: "authp",
                table: "Tenants",
                column: "ParentTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantFullName",
                schema: "authp",
                table: "Tenants",
                column: "TenantFullName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserToRoles_RoleName",
                schema: "authp",
                table: "UserToRoles",
                column: "RoleName");

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //Manually added the triggers for the concurrency checks
            //see Test\UnitCommands\RunThisToGetExtraMigrateParts which creates the code below

            migrationBuilder.Sql(
                @"CREATE TRIGGER UpdateAuthUsersVersion
    AFTER UPDATE ON AuthUsers
    BEGIN
UPDATE AuthUsers
SET Version = Version + 1
WHERE rowid = NEW.rowid;
END;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER UpdateRefreshTokensVersion
    AFTER UPDATE ON RefreshTokens
    BEGIN
UPDATE RefreshTokens
SET Version = Version + 1
WHERE rowid = NEW.rowid;
END;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER UpdateRoleToPermissionsVersion
    AFTER UPDATE ON RoleToPermissions
    BEGIN
UPDATE RoleToPermissions
SET Version = Version + 1
WHERE rowid = NEW.rowid;
END;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER UpdateTenantsVersion
    AFTER UPDATE ON Tenants
    BEGIN
UPDATE Tenants
SET Version = Version + 1
WHERE rowid = NEW.rowid;
END;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER UpdateUserToRolesVersion
    AFTER UPDATE ON UserToRoles
    BEGIN
UPDATE UserToRoles
SET Version = Version + 1
WHERE rowid = NEW.rowid;
END;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER UpdateRoleToPermissionsTenantVersion
    AFTER UPDATE ON RoleToPermissionsTenant
    BEGIN
UPDATE RoleToPermissionsTenant
SET Version = Version + 1
WHERE rowid = NEW.rowid;
END;");


        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "authp");

            migrationBuilder.DropTable(
                name: "RoleToPermissionsTenant",
                schema: "authp");

            migrationBuilder.DropTable(
                name: "UserToRoles",
                schema: "authp");

            migrationBuilder.DropTable(
                name: "AuthUsers",
                schema: "authp");

            migrationBuilder.DropTable(
                name: "RoleToPermissions",
                schema: "authp");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "authp");
        }
    }
}
