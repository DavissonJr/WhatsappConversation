using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsappCrmIA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInternalAiCreditsBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiCreditsBalanceUsd",
                table: "Tenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AiCreditsBalanceUsd",
                table: "Tenants",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
