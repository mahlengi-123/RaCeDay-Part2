using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaCeDay.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventBannerColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrolments_Categories_CategoryId",
                table: "Enrolments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrolments_Events_EventId",
                table: "Enrolments");

            migrationBuilder.AddColumn<string>(
                name: "BannerBlobName",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerImageURL",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrolments_Categories_CategoryId",
                table: "Enrolments",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrolments_Events_EventId",
                table: "Enrolments",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrolments_Categories_CategoryId",
                table: "Enrolments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrolments_Events_EventId",
                table: "Enrolments");

            migrationBuilder.DropColumn(
                name: "BannerBlobName",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BannerImageURL",
                table: "Events");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrolments_Categories_CategoryId",
                table: "Enrolments",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrolments_Events_EventId",
                table: "Enrolments",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
.