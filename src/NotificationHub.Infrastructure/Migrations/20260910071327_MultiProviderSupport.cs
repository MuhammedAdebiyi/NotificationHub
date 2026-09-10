using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MultiProviderSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "EmailProviderConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropIndex(
                name: "IX_EmailProviderConfigs_OrganizationId",
                table: "EmailProviderConfigs");

            migrationBuilder.CreateIndex(
                name: "IX_EmailProviderConfigs_OrganizationId_ProviderType",
                table: "EmailProviderConfigs",
                columns: new[] { "OrganizationId", "ProviderType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailProviderConfigs_OrganizationId_ProviderType",
                table: "EmailProviderConfigs");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "EmailProviderConfigs");

            migrationBuilder.CreateIndex(
                name: "IX_EmailProviderConfigs_OrganizationId",
                table: "EmailProviderConfigs",
                column: "OrganizationId",
                unique: true);
        }
    }
}
