using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberRevokedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "OrganizationMembers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "OrganizationMembers");
        }
    }
}
