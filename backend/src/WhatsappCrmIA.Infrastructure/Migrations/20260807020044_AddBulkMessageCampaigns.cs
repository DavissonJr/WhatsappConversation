using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsappCrmIA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkMessageCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkMessageCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MessageText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    WhatsAppConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelaySeconds = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalRecipients = table.Column<int>(type: "integer", nullable: false),
                    SentCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkMessageCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkMessageCampaigns_WhatsAppConnections_WhatsAppConnection~",
                        column: x => x.WhatsAppConnectionId,
                        principalTable: "WhatsAppConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BulkMessageRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkMessageRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkMessageRecipients_BulkMessageCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "BulkMessageCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BulkMessageRecipients_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessageCampaigns_WhatsAppConnectionId",
                table: "BulkMessageCampaigns",
                column: "WhatsAppConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessageRecipients_CampaignId",
                table: "BulkMessageRecipients",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessageRecipients_ContactId",
                table: "BulkMessageRecipients",
                column: "ContactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkMessageRecipients");

            migrationBuilder.DropTable(
                name: "BulkMessageCampaigns");
        }
    }
}
