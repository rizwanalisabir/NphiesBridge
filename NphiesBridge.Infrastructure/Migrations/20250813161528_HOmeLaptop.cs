using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NphiesBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HOmeLaptop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HealthProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthProviders", x => x.Id);
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
                name: "NphiesServiceCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NphiesServiceCodeValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_HealthProviders_HealthProviderId",
                        column: x => x.HealthProviderId,
                        principalTable: "HealthProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MappingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_MappingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MappingSessions_HealthProviders_HealthProviderId",
                        column: x => x.HealthProviderId,
                        principalTable: "HealthProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    MappingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_HospitalIcdCodes_MappingSessions_MappingSessionId",
                        column: x => x.MappingSessionId,
                        principalTable: "MappingSessions",
                        principalColumn: "Id");
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

            migrationBuilder.CreateTable(
                name: "IcdCodeMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NphiesIcdCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HealthProviderIcdCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MappedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MappedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAiSuggested = table.Column<bool>(type: "bit", nullable: false),
                    ConfidenceScore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MappingSessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalIcdCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IcdCodeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IcdCodeMappings_HospitalIcdCodes_HospitalIcdCodeId",
                        column: x => x.HospitalIcdCodeId,
                        principalTable: "HospitalIcdCodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceCodeMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NphiesServiceCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    HealthProviderServiceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    HealthProviderServiceRelation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MappedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MappedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAiSuggested = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ConfidenceScore = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ServiceMappingSessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthProviderServiceCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCodeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCodeMappings_HealthProviderServiceCodes_HealthProviderServiceCodeId",
                        column: x => x.HealthProviderServiceCodeId,
                        principalTable: "HealthProviderServiceCodes",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("49dacab5-1661-4139-8d0d-7fe53ed5b5e5"), null, "Healthcare Provider User", "Provider", "PROVIDER" },
                    { new Guid("e3761bda-73d3-4b48-9891-80a75062804c"), null, "System Administrator", "Admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_HealthProviderId",
                table: "AspNetUsers",
                column: "HealthProviderId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProviders_LicenseNumber",
                table: "HealthProviders",
                column: "LicenseNumber");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProviderServiceCodes_HealthProviderId",
                table: "HealthProviderServiceCodes",
                column: "HealthProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProviderServiceCodes_ServiceMappingSessionId",
                table: "HealthProviderServiceCodes",
                column: "ServiceMappingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalIcdCodes_HealthProviderId",
                table: "HospitalIcdCodes",
                column: "HealthProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalIcdCodes_MappingSessionId",
                table: "HospitalIcdCodes",
                column: "MappingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_IcdCodeMappings_HospitalIcdCodeId",
                table: "IcdCodeMappings",
                column: "HospitalIcdCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MappingSessions_HealthProviderId",
                table: "MappingSessions",
                column: "HealthProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodeMappings_HealthProviderServiceCodeId",
                table: "ServiceCodeMappings",
                column: "HealthProviderServiceCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMappingSessions_HealthProviderId",
                table: "ServiceMappingSessions",
                column: "HealthProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "IcdCodeMappings");

            migrationBuilder.DropTable(
                name: "NphiesIcdCodes");

            migrationBuilder.DropTable(
                name: "NphiesServiceCodes");

            migrationBuilder.DropTable(
                name: "ServiceCodeMappings");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "HospitalIcdCodes");

            migrationBuilder.DropTable(
                name: "HealthProviderServiceCodes");

            migrationBuilder.DropTable(
                name: "MappingSessions");

            migrationBuilder.DropTable(
                name: "ServiceMappingSessions");

            migrationBuilder.DropTable(
                name: "HealthProviders");
        }
    }
}
