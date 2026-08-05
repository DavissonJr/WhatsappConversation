using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsappCrmIA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowCascadeDeleteOnWhatsAppConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_WhatsAppConnections_WhatsAppConnectionId",
                table: "Conversations");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_WhatsAppConnections_WhatsAppConnectionId",
                table: "Conversations",
                column: "WhatsAppConnectionId",
                principalTable: "WhatsAppConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_WhatsAppConnections_WhatsAppConnectionId",
                table: "Conversations");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_WhatsAppConnections_WhatsAppConnectionId",
                table: "Conversations",
                column: "WhatsAppConnectionId",
                principalTable: "WhatsAppConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
