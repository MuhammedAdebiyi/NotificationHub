using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipientOptOuts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecipientOptOuts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    OptedOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipientOptOuts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipientOptOuts_OrganizationId_Email",
                table: "RecipientOptOuts",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipientOptOuts");
        }
    }
}
