using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NphiesBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("52ab66bd-41e6-4d91-b416-0a8ed925f305"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("c39dc88b-f909-4a99-bb1d-27a86157219f"));

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

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("2f578132-a52f-453f-acdc-3c0e1df6540b"), null, "Healthcare Provider User", "Provider", "PROVIDER" },
                    { new Guid("3a37ffbe-719f-42fd-a0eb-abc9c439bf4a"), null, "System Administrator", "Admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NphiesServiceCodes");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("2f578132-a52f-453f-acdc-3c0e1df6540b"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("3a37ffbe-719f-42fd-a0eb-abc9c439bf4a"));

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("52ab66bd-41e6-4d91-b416-0a8ed925f305"), null, "Healthcare Provider User", "Provider", "PROVIDER" },
                    { new Guid("c39dc88b-f909-4a99-bb1d-27a86157219f"), null, "System Administrator", "Admin", "ADMIN" }
                });
        }
    }
}
