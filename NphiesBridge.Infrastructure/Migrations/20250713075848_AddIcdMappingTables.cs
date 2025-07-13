using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NphiesBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIcdMappingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("4f24ba67-94fc-46e1-bb4b-a8d47eef4bf5"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("67081e4d-694e-47d7-9bd5-f462998cac69"));

            migrationBuilder.CreateTable(
                name: "HospitalIcdCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiagnosisName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiagnosisDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestedIcd10Am = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMapped = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalIcdCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HospitalIcdCodes_HealthProviders_HealthProviderId",
                        column: x => x.HealthProviderId,
                        principalTable: "HealthProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NphiesIcdCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Chapter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NphiesIcdCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IcdCodeMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NphiesIcdCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MappedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MappedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAiSuggested = table.Column<bool>(type: "bit", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IcdCodeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IcdCodeMappings_AspNetUsers_MappedByUserId",
                        column: x => x.MappedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IcdCodeMappings_HospitalIcdCodes_HospitalCodeId",
                        column: x => x.HospitalCodeId,
                        principalTable: "HospitalIcdCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("df0848e0-640b-49f8-87d1-12a486659396"), null, "Healthcare Provider User", "Provider", "PROVIDER" },
                    { new Guid("e359867e-016a-40a4-934d-6852e36d13d2"), null, "System Administrator", "Admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalIcdCodes_HealthProviderId",
                table: "HospitalIcdCodes",
                column: "HealthProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_IcdCodeMappings_HospitalCodeId",
                table: "IcdCodeMappings",
                column: "HospitalCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_IcdCodeMappings_MappedByUserId",
                table: "IcdCodeMappings",
                column: "MappedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IcdCodeMappings");

            migrationBuilder.DropTable(
                name: "NphiesIcdCodes");

            migrationBuilder.DropTable(
                name: "HospitalIcdCodes");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("df0848e0-640b-49f8-87d1-12a486659396"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("e359867e-016a-40a4-934d-6852e36d13d2"));

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("4f24ba67-94fc-46e1-bb4b-a8d47eef4bf5"), null, "Healthcare Provider User", "Provider", "PROVIDER" },
                    { new Guid("67081e4d-694e-47d7-9bd5-f462998cac69"), null, "System Administrator", "Admin", "ADMIN" }
                });
        }
    }
}
