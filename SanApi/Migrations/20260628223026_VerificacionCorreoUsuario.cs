using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanApi.Migrations
{
    /// <inheritdoc />
    public partial class VerificacionCorreoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoVerificacion",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CorreoVerificado",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoVerificacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CorreoVerificado",
                table: "Usuarios");
        }
    }
}
