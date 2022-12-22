using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosmosProposalBot.Migrations
{
    /// <inheritdoc />
    public partial class AddedCustomChainRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CustomForGuildId",
                table: "Chains",
                type: "decimal(20,0)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomForGuildId",
                table: "Chains");
        }
    }
}
