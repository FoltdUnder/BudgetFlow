using System;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetFlow.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260319141000_AddGlobalDefaultCategories")]
    public partial class AddGlobalDefaultCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_users_UserId",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_UserId_Name_Type",
                table: "categories");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "categories",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name_Type",
                table: "categories",
                columns: new[] { "Name", "Type" },
                unique: true,
                filter: "\"IsDefault\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_categories_UserId_Name_Type",
                table: "categories",
                columns: new[] { "UserId", "Name", "Type" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_users_UserId",
                table: "categories",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_users_UserId",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_Name_Type",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_UserId_Name_Type",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "categories");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "categories",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_UserId_Name_Type",
                table: "categories",
                columns: new[] { "UserId", "Name", "Type" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_categories_users_UserId",
                table: "categories",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
