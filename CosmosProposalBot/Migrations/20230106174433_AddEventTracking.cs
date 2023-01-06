using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosmosProposalBot.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackedEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Height = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    HeightEstimatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackedEvents_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackedEventThread",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackedEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThreadId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    GuildId = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedEventThread", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackedEventThread_TrackedEvents_TrackedEventId",
                        column: x => x.TrackedEventId,
                        principalTable: "TrackedEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackedEvents_ProposalId",
                table: "TrackedEvents",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedEventThread_TrackedEventId",
                table: "TrackedEventThread",
                column: "TrackedEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackedEventThread");

            migrationBuilder.DropTable(
                name: "TrackedEvents");
        }
    }
}
