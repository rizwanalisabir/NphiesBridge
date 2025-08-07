using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NphiesBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removedFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IcdCodeMappings_AspNetUsers_MappedByUserId",
                table: "IcdCodeMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_IcdCodeMappings_HospitalIcdCodes_HospitalCodeId",
                table: "IcdCodeMappings");

            migrationBuilder.DropIndex(
                name: "IX_IcdCodeMappings_HospitalCodeId",
                table: "IcdCodeMappings");

            migrationBuilder.DropIndex(
                name: "IX_IcdCodeMappings_MappedByUserId",
                table: "IcdCodeMappings");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("9c4caa44-52bf-4a52-afab-9a200369ce67"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("cec2823a-ab32-4998-9ab6-b782b48b7d40"));

            migrationBuilder.DropColumn(
                name: "HospitalCodeId",
                table: "IcdCodeMappings");

            migrationBuilder.AddColumn<Guid>(
                name: "HospitalIcdCodeId",
                table: "IcdCodeMappings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("4e84c39b-89d6-4944-8ca2-c79d87bd908d"), null, "System Administrator", "Admin", "ADMIN" },
                    { new Guid("677d5c26-e9ca-4ffa-a920-b023ce537aeb"), null, "Healthcare Provider User", "Provider", "PROVIDER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_IcdCodeMappings_HospitalIcdCodeId",
                table: "IcdCodeMappings",
                column: "HospitalIcdCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_IcdCodeMappings_HospitalIcdCodes_HospitalIcdCodeId",
                table: "IcdCodeMappings",
                column: "HospitalIcdCodeId",
                principalTable: "HospitalIcdCodes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IcdCodeMappings_HospitalIcdCodes_HospitalIcdCodeId",
                table: "IcdCodeMappings");

            migrationBuilder.DropIndex(
                name: "IX_IcdCodeMappings_HospitalIcdCodeId",
                table: "IcdCodeMappings");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("4e84c39b-89d6-4944-8ca2-c79d87bd908d"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("677d5c26-e9ca-4ffa-a920-b023ce537aeb"));

            migrationBuilder.DropColumn(
                name: "HospitalIcdCodeId",
                table: "IcdCodeMappings");

            migrationBuilder.AddColumn<Guid>(
                name: "HospitalCodeId",
                table: "IcdCodeMappings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("9c4caa44-52bf-4a52-afab-9a200369ce67"), null, "System Administrator", "Admin", "ADMIN" },
                    { new Guid("cec2823a-ab32-4998-9ab6-b782b48b7d40"), null, "Healthcare Provider User", "Provider", "PROVIDER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_IcdCodeMappings_HospitalCodeId",
                table: "IcdCodeMappings",
                column: "HospitalCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_IcdCodeMappings_MappedByUserId",
                table: "IcdCodeMappings",
                column: "MappedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_IcdCodeMappings_AspNetUsers_MappedByUserId",
                table: "IcdCodeMappings",
                column: "MappedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IcdCodeMappings_HospitalIcdCodes_HospitalCodeId",
                table: "IcdCodeMappings",
                column: "HospitalCodeId",
                principalTable: "HospitalIcdCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
