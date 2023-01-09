using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosmosProposalBot.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedNotificationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextNotificationAt",
                table: "TrackedEvents");

            migrationBuilder.AddColumn<long>(
                name: "NextNotificationAtSecondsLeft",
                table: "TrackedEvents",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextNotificationAtSecondsLeft",
                table: "TrackedEvents");

            migrationBuilder.AddColumn<DateTime>(
                name: "NextNotificationAt",
                table: "TrackedEvents",
                type: "datetime2",
                nullable: true);
        }
    }
}
