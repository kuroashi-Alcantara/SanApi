using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanApi.Migrations
{
    /// <inheritdoc />
    public partial class AddComprobanteDesembolsoAPeriodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UrlComprobanteDesembolso",
                table: "Periodos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UrlComprobanteDesembolso",
                table: "Periodos");
        }
    }
}
