using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregadasReglasDeNegocioSala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PermiteDesembolsoAnticipado",
                table: "Salas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SorteoTurnosAleatorio",
                table: "Salas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermiteDesembolsoAnticipado",
                table: "Salas");

            migrationBuilder.DropColumn(
                name: "SorteoTurnosAleatorio",
                table: "Salas");
        }
    }
}
