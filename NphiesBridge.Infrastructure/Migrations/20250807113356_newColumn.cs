using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NphiesBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("3998c9b7-aaba-475a-9737-b1f1fb6d4d36"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("74ea35df-eac7-43f1-ac4b-c4045f0a4331"));

            migrationBuilder.AddColumn<string>(
                name: "HealthProviderIcdCode",
                table: "IcdCodeMappings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("9c4caa44-52bf-4a52-afab-9a200369ce67"), null, "System Administrator", "Admin", "ADMIN" },
                    { new Guid("cec2823a-ab32-4998-9ab6-b782b48b7d40"), null, "Healthcare Provider User", "Provider", "PROVIDER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("9c4caa44-52bf-4a52-afab-9a200369ce67"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("cec2823a-ab32-4998-9ab6-b782b48b7d40"));

            migrationBuilder.DropColumn(
                name: "HealthProviderIcdCode",
                table: "IcdCodeMappings");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("3998c9b7-aaba-475a-9737-b1f1fb6d4d36"), null, "System Administrator", "Admin", "ADMIN" },
                    { new Guid("74ea35df-eac7-43f1-ac4b-c4045f0a4331"), null, "Healthcare Provider User", "Provider", "PROVIDER" }
                });
        }
    }
}
