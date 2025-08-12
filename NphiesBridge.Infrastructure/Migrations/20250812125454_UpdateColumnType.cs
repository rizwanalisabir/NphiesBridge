using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NphiesBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("4e84c39b-89d6-4944-8ca2-c79d87bd908d"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("677d5c26-e9ca-4ffa-a920-b023ce537aeb"));

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                table: "MappingSessions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("27d51ca0-6662-4656-bed2-9530e084a6cb"), null, "System Administrator", "Admin", "ADMIN" },
                    { new Guid("3ad9b6e9-18e2-4e27-aeb6-5fa6c9ebf9d2"), null, "Healthcare Provider User", "Provider", "PROVIDER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("27d51ca0-6662-4656-bed2-9530e084a6cb"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("3ad9b6e9-18e2-4e27-aeb6-5fa6c9ebf9d2"));

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "MappingSessions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("4e84c39b-89d6-4944-8ca2-c79d87bd908d"), null, "System Administrator", "Admin", "ADMIN" },
                    { new Guid("677d5c26-e9ca-4ffa-a920-b023ce537aeb"), null, "Healthcare Provider User", "Provider", "PROVIDER" }
                });
        }
    }
}
