using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NphiesBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewEntitiesForServiceMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("2d95de42-0f69-482c-a4a4-5178232f98bf"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("4ff3b76b-382f-43fb-930e-ada165b8ba9e"));

            migrationBuilder.CreateTable(
                name: "NphiesServiceCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NphiesServiceCodeValue = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NphiesServiceDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NphiesServiceCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCodeMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderItemRelation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderItemId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderItemName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NphiesServiceCodeValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MappedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MappedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAiSuggested = table.Column<bool>(type: "bit", nullable: false),
                    ConfidenceScore = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCodeMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCodesMappingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ProcessedRows = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemRelation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    NphiesCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NphiesDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMapped = table.Column<bool>(type: "bit", nullable: false),
                    SuggestedNphiesCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    MatchReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                name: "IX_ServiceCodeMappings_HealthProviderId_ProviderItemRelation",
                table: "ServiceCodeMappings",
                columns: new[] { "HealthProviderId", "ProviderItemRelation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodesMappingSessions_SessionId",
                table: "ServiceCodesMappingSessions",
                column: "SessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NphiesServiceCodes");

            migrationBuilder.DropTable(
                name: "ProviderServiceItems");

            migrationBuilder.DropTable(
                name: "ServiceCodeMappings");

            migrationBuilder.DropTable(
                name: "ServiceCodesMappingSessions");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("356d86cd-78e4-4fbf-84c5-ebdc9952d301"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("ec5aea0b-0fe2-4bf1-8526-15d862455d5e"));

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("2d95de42-0f69-482c-a4a4-5178232f98bf"), null, "System Administrator", "Admin", "ADMIN" },
                    { new Guid("4ff3b76b-382f-43fb-930e-ada165b8ba9e"), null, "Healthcare Provider User", "Provider", "PROVIDER" }
                });
        }
    }
}
