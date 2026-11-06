using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReplyToAndSenderResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReplyTo",
                table: "Templates",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderEmail",
                table: "Templates",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderName",
                table: "Templates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplyTo",
                table: "Notifications",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplyToSource",
                table: "Notifications",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ProviderDefault");

            migrationBuilder.AddColumn<string>(
                name: "SenderEmail",
                table: "Notifications",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderName",
                table: "Notifications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderSource",
                table: "Notifications",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ProviderDefault");

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TemplateId",
                table: "Notifications",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Templates_TemplateId",
                table: "Notifications",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Templates_TemplateId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TemplateId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReplyTo",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "SenderEmail",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "SenderName",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "ReplyTo",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReplyToSource",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SenderEmail",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SenderName",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SenderSource",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "Notifications");
        }
    }
}
