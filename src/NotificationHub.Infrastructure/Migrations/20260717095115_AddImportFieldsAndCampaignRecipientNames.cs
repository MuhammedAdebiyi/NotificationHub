using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportFieldsAndCampaignRecipientNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColumnMapping",
                table: "ImportJobs");

            migrationBuilder.RenameColumn(
                name: "NotificationsCreated",
                table: "ImportJobs",
                newName: "RecipientsAdded");

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "ImportJobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "EmailColumn",
                table: "ImportJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstNameColumn",
                table: "ImportJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastNameColumn",
                table: "ImportJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryKeyColumn",
                table: "ImportJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhereClause",
                table: "ImportJobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "CampaignRecipients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "CampaignRecipients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CampaignId",
                table: "ImportJobs",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportJobs_Campaigns_CampaignId",
                table: "ImportJobs",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportJobs_Campaigns_CampaignId",
                table: "ImportJobs");

            migrationBuilder.DropIndex(
                name: "IX_ImportJobs_CampaignId",
                table: "ImportJobs");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "ImportJobs");

            migrationBuilder.DropColumn(
                name: "EmailColumn",
                table: "ImportJobs");

            migrationBuilder.DropColumn(
                name: "FirstNameColumn",
                table: "ImportJobs");

            migrationBuilder.DropColumn(
                name: "LastNameColumn",
                table: "ImportJobs");

            migrationBuilder.DropColumn(
                name: "PrimaryKeyColumn",
                table: "ImportJobs");

            migrationBuilder.DropColumn(
                name: "WhereClause",
                table: "ImportJobs");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "CampaignRecipients");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "CampaignRecipients");

            migrationBuilder.RenameColumn(
                name: "RecipientsAdded",
                table: "ImportJobs",
                newName: "NotificationsCreated");

            migrationBuilder.AddColumn<string>(
                name: "ColumnMapping",
                table: "ImportJobs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
