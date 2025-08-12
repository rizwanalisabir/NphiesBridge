using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NphiesBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("27d51ca0-6662-4656-bed2-9530e084a6cb"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("3ad9b6e9-18e2-4e27-aeb6-5fa6c9ebf9d2"));

            migrationBuilder.AddColumn<Guid>(
                name: "MappingSessionID",
                table: "IcdCodeMappings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("2d95de42-0f69-482c-a4a4-5178232f98bf"), null, "System Administrator", "Admin", "ADMIN" },
                    { new Guid("4ff3b76b-382f-43fb-930e-ada165b8ba9e"), null, "Healthcare Provider User", "Provider", "PROVIDER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("2d95de42-0f69-482c-a4a4-5178232f98bf"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("4ff3b76b-382f-43fb-930e-ada165b8ba9e"));

            migrationBuilder.DropColumn(
                name: "MappingSessionID",
                table: "IcdCodeMappings");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("27d51ca0-6662-4656-bed2-9530e084a6cb"), null, "System Administrator", "Admin", "ADMIN" },
                    { new Guid("3ad9b6e9-18e2-4e27-aeb6-5fa6c9ebf9d2"), null, "Healthcare Provider User", "Provider", "PROVIDER" }
                });
        }
    }
}
