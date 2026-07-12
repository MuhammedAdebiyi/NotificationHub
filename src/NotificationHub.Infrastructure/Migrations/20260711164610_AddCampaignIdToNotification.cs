using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignIdToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Campaigns",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CampaignId",
                table: "Notifications",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Campaigns_CampaignId",
                table: "Notifications",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Campaigns_CampaignId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_CampaignId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Campaigns");
        }
    }
}
