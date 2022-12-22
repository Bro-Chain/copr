using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosmosProposalBot.Migrations
{
    /// <inheritdoc />
    public partial class AddedLinkPattern : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkPattern",
                table: "Chains",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkPattern",
                table: "Chains");
        }
    }
}
