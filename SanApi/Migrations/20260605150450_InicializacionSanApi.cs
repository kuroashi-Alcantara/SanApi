using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanApi.Migrations
{
    /// <inheritdoc />
    public partial class InicializacionSanApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContrasenaHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    ScoreRiesgo = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Salas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreSala = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MontoCuota = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Frecuencia = table.Column<int>(type: "int", nullable: false),
                    CantidadParticipantes = table.Column<int>(type: "int", nullable: false),
                    EsPublica = table.Column<bool>(type: "bit", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Salas_Usuarios_CreadorId",
                        column: x => x.CreadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ParticipantesSala",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroTurno = table.Column<int>(type: "int", nullable: false),
                    EstadoParticipacion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantesSala", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantesSala_Salas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipantesSala_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Periodos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroRonda = table.Column<int>(type: "int", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BeneficiarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstadoPeriodo = table.Column<int>(type: "int", nullable: false),
                    FechaDesembolso = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Periodos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Periodos_Salas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Periodos_Usuarios_BeneficiarioId",
                        column: x => x.BeneficiarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Transacciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioPagadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UrlVoucher = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EstadoPago = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transacciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transacciones_Periodos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalTable: "Periodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transacciones_Usuarios_UsuarioPagadorId",
                        column: x => x.UsuarioPagadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantesSala_SalaId",
                table: "ParticipantesSala",
                column: "SalaId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantesSala_UsuarioId",
                table: "ParticipantesSala",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Periodos_BeneficiarioId",
                table: "Periodos",
                column: "BeneficiarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Periodos_SalaId",
                table: "Periodos",
                column: "SalaId");

            migrationBuilder.CreateIndex(
                name: "IX_Salas_CreadorId",
                table: "Salas",
                column: "CreadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_PeriodoId",
                table: "Transacciones",
                column: "PeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_UsuarioPagadorId",
                table: "Transacciones",
                column: "UsuarioPagadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParticipantesSala");

            migrationBuilder.DropTable(
                name: "Transacciones");

            migrationBuilder.DropTable(
                name: "Periodos");

            migrationBuilder.DropTable(
                name: "Salas");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
