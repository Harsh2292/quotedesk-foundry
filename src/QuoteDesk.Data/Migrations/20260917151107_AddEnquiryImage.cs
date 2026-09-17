using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuoteDesk.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnquiryImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageDataUrl",
                table: "Enquiries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageDataUrl",
                table: "Enquiries");
        }
    }
}
