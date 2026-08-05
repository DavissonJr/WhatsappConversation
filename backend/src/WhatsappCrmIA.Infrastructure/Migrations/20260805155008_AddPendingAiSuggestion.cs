using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsappCrmIA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingAiSuggestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingAiSuggestion",
                table: "Conversations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingAiSuggestion",
                table: "Conversations");
        }
    }
}
