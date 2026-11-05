using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHelpdesk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalReviewedAtUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalReviewedByUserId",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ApprovalReviewedByUserId",
                table: "Users",
                column: "ApprovalReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ApprovalStatus",
                table: "Users",
                column: "ApprovalStatus");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ApprovalReviewedByUserId",
                table: "Users",
                column: "ApprovalReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ApprovalReviewedByUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ApprovalReviewedByUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ApprovalStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovalReviewedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovalReviewedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);
        }
    }
}
