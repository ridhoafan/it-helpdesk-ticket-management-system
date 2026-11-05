using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHelpdesk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddITSupportSpecializations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ITSupportCategories",
                columns: table => new
                {
                    ITSupportUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TicketCategoryId = table.Column<int>(type: "int", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITSupportCategories", x => new { x.ITSupportUserId, x.TicketCategoryId });
                    table.ForeignKey(
                        name: "FK_ITSupportCategories_TicketCategories_TicketCategoryId",
                        column: x => x.TicketCategoryId,
                        principalTable: "TicketCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ITSupportCategories_Users_ITSupportUserId",
                        column: x => x.ITSupportUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ITSupportCategories_TicketCategoryId",
                table: "ITSupportCategories",
                column: "TicketCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ITSupportCategories");
        }
    }
}
