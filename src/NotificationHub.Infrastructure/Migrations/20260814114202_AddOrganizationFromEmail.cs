using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationFromEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FromEmail",
                table: "Organizations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "campaigns@coursevaultai.app");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromEmail",
                table: "Organizations");
        }
    }
}
