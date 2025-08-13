using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NphiesBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewEntitiesForServiceMappings2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NphiesServiceCodes");

            migrationBuilder.DropTable(
                name: "ProviderServiceItems");

            migrationBuilder.DropTable(
                name: "ServiceCodesMappingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCodeMappings_HealthProviderId_ProviderItemRelation",
                table: "ServiceCodeMappings");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("356d86cd-78e4-4fbf-84c5-ebdc9952d301"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("ec5aea0b-0fe2-4bf1-8526-15d862455d5e"));

            migrationBuilder.DropColumn(
                name: "ProviderItemId",
                table: "ServiceCodeMappings");

            migrationBuilder.DropColumn(
                name: "ProviderItemName",
                table: "ServiceCodeMappings");

            migrationBuilder.RenameColumn(
                name: "ProviderItemRelation",
                table: "ServiceCodeMappings",
                newName: "NphiesServiceCode");

            migrationBuilder.RenameColumn(
                name: "NphiesServiceCodeValue",
                table: "ServiceCodeMappings",
                newName: "HealthProviderServiceRelation");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "ServiceCodeMappings",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAiSuggested",
                table: "ServiceCodeMappings",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ServiceCodeMappings",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ConfidenceScore",
                table: "ServiceCodeMappings",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HealthProviderServiceCodeId",
                table: "ServiceCodeMappings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthProviderServiceId",
                table: "ServiceCodeMappings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceMappingSessionID",
                table: "ServiceCodeMappings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ServiceMappingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ProcessedRows = table.Column<int>(type: "int", nullable: false),
                    CompletedRows = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceMappingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceMappingSessions_HealthProviders_HealthProviderId",
                        column: x => x.HealthProviderId,
                        principalTable: "HealthProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthProviderServiceCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthProviderServiceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    HealthProviderServiceRelation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    HealthProviderServiceName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    NphiesServiceCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsMapped = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ServiceMappingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthProviderServiceCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthProviderServiceCodes_HealthProviders_HealthProviderId",
                        column: x => x.HealthProviderId,
                        principalTable: "HealthProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthProviderServiceCodes_ServiceMappingSessions_ServiceMappingSessionId",
                        column: x => x.ServiceMappingSessionId,
                        principalTable: "ServiceMappingSessions",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("52ab66bd-41e6-4d91-b416-0a8ed925f305"), null, "Healthcare Provider User", "Provider", "PROVIDER" },
                    { new Guid("c39dc88b-f909-4a99-bb1d-27a86157219f"), null, "System Administrator", "Admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodeMappings_HealthProviderServiceCodeId",
                table: "ServiceCodeMappings",
                column: "HealthProviderServiceCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProviderServiceCodes_HealthProviderId",
                table: "HealthProviderServiceCodes",
                column: "HealthProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProviderServiceCodes_ServiceMappingSessionId",
                table: "HealthProviderServiceCodes",
                column: "ServiceMappingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMappingSessions_HealthProviderId",
                table: "ServiceMappingSessions",
                column: "HealthProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceCodeMappings_HealthProviderServiceCodes_HealthProviderServiceCodeId",
                table: "ServiceCodeMappings",
                column: "HealthProviderServiceCodeId",
                principalTable: "HealthProviderServiceCodes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceCodeMappings_HealthProviderServiceCodes_HealthProviderServiceCodeId",
                table: "ServiceCodeMappings");

            migrationBuilder.DropTable(
                name: "HealthProviderServiceCodes");

            migrationBuilder.DropTable(
                name: "ServiceMappingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCodeMappings_HealthProviderServiceCodeId",
                table: "ServiceCodeMappings");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("52ab66bd-41e6-4d91-b416-0a8ed925f305"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("c39dc88b-f909-4a99-bb1d-27a86157219f"));

            migrationBuilder.DropColumn(
                name: "HealthProviderServiceCodeId",
                table: "ServiceCodeMappings");

            migrationBuilder.DropColumn(
                name: "HealthProviderServiceId",
                table: "ServiceCodeMappings");

            migrationBuilder.DropColumn(
                name: "ServiceMappingSessionID",
                table: "ServiceCodeMappings");

            migrationBuilder.RenameColumn(
                name: "NphiesServiceCode",
                table: "ServiceCodeMappings",
                newName: "ProviderItemRelation");

            migrationBuilder.RenameColumn(
                name: "HealthProviderServiceRelation",
                table: "ServiceCodeMappings",
                newName: "NphiesServiceCodeValue");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "ServiceCodeMappings",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAiSuggested",
                table: "ServiceCodeMappings",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ServiceCodeMappings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "ConfidenceScore",
                table: "ServiceCodeMappings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<string>(
                name: "ProviderItemId",
                table: "ServiceCodeMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderItemName",
                table: "ServiceCodeMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NphiesServiceCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    NphiesServiceCodeValue = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NphiesServiceDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NphiesServiceCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCodesMappingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedRows = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCodesMappingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderServiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCodesMappingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsMapped = table.Column<bool>(type: "bit", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemRelation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MatchReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    NphiesCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NphiesDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestedNphiesCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderServiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderServiceItems_ServiceCodesMappingSessions_ServiceCodesMappingSessionId",
                        column: x => x.ServiceCodesMappingSessionId,
                        principalTable: "ServiceCodesMappingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("356d86cd-78e4-4fbf-84c5-ebdc9952d301"), null, "System Administrator", "Admin", "ADMIN" },
                    { new Guid("ec5aea0b-0fe2-4bf1-8526-15d862455d5e"), null, "Healthcare Provider User", "Provider", "PROVIDER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodeMappings_HealthProviderId_ProviderItemRelation",
                table: "ServiceCodeMappings",
                columns: new[] { "HealthProviderId", "ProviderItemRelation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NphiesServiceCodes_NphiesServiceCodeValue",
                table: "NphiesServiceCodes",
                column: "NphiesServiceCodeValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderServiceItems_ServiceCodesMappingSessionId_ItemRelation",
                table: "ProviderServiceItems",
                columns: new[] { "ServiceCodesMappingSessionId", "ItemRelation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodesMappingSessions_SessionId",
                table: "ServiceCodesMappingSessions",
                column: "SessionId",
                unique: true);
        }
    }
}
