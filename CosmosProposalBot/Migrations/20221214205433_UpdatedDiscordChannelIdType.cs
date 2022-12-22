using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosmosProposalBot.Migrations
{
    public partial class UpdatedDiscordChannelIdType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordChannelId",
                table: "ChannelSubscriptions",
                type: "decimal(20,0)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "ChannelSubscriptions",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "ChannelSubscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "DiscordChannelId",
                table: "ChannelSubscriptions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)");
        }
    }
}
