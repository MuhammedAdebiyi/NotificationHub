using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignStartNotificationSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartNotificationSentAt",
                table: "Campaigns",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartNotificationSentAt",
                table: "Campaigns");
        }
    }
}
