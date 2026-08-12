using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryAdvanced.Migrations
{
    /// <inheritdoc />
    public partial class updateLoanTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "LoanTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedDate",
                table: "LoanTickets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "LoanTickets");

            migrationBuilder.DropColumn(
                name: "ReturnedDate",
                table: "LoanTickets");
        }
    }
}
