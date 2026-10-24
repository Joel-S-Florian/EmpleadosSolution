using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Empleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class CicloNomina_Recibos_Prestamos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nominas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Quincena = table.Column<int>(type: "int", nullable: false),
                    Frecuencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalIngresos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeducciones = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalNeto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAportesPatronales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadaPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FechaCalculo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AprobadaPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PagadaPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nominas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nominas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Prestamos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    Concepto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoCuota = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoloFinDeMes = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestamos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prestamos_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecibosPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    NominaId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    NombreEmpleado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Nss = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Departamento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Puesto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Salario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TipoSalario = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NumeroCuenta = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    HorasTrabajadas = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    DiasAusenciaSinPago = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    HorasExtra = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    HorasExtraExceso = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    HorasNocturnas = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    HorasFeriado = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    TotalIngresos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeduccionesLey = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeduccionesInternas = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Neto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAportesPatronales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseTss = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseIsrMensual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AcumuladoJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExplicacionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdvertenciasJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErroresJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecibosPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecibosPago_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecibosPago_Nominas_NominaId",
                        column: x => x.NominaId,
                        principalTable: "Nominas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineasReciboPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReciboId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Concepto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    CotizaTss = table.Column<bool>(type: "bit", nullable: false),
                    GravaIsr = table.Column<bool>(type: "bit", nullable: false),
                    PrestamoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasReciboPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasReciboPago_Prestamos_PrestamoId",
                        column: x => x.PrestamoId,
                        principalTable: "Prestamos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineasReciboPago_RecibosPago_ReciboId",
                        column: x => x.ReciboId,
                        principalTable: "RecibosPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NovedadesNomina",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReciboId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Concepto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CotizaTss = table.Column<bool>(type: "bit", nullable: false),
                    GravaIsr = table.Column<bool>(type: "bit", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovedadesNomina", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NovedadesNomina_RecibosPago_ReciboId",
                        column: x => x.ReciboId,
                        principalTable: "RecibosPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LineasReciboPago_PrestamoId",
                table: "LineasReciboPago",
                column: "PrestamoId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasReciboPago_ReciboId",
                table: "LineasReciboPago",
                column: "ReciboId");

            migrationBuilder.CreateIndex(
                name: "IX_Nominas_EmpresaId_Anio_Mes_Frecuencia_Quincena",
                table: "Nominas",
                columns: new[] { "EmpresaId", "Anio", "Mes", "Frecuencia", "Quincena" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NovedadesNomina_ReciboId",
                table: "NovedadesNomina",
                column: "ReciboId");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_EmpleadoId_Activo",
                table: "Prestamos",
                columns: new[] { "EmpleadoId", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_RecibosPago_EmpleadoId",
                table: "RecibosPago",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecibosPago_NominaId_EmpleadoId",
                table: "RecibosPago",
                columns: new[] { "NominaId", "EmpleadoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineasReciboPago");

            migrationBuilder.DropTable(
                name: "NovedadesNomina");

            migrationBuilder.DropTable(
                name: "Prestamos");

            migrationBuilder.DropTable(
                name: "RecibosPago");

            migrationBuilder.DropTable(
                name: "Nominas");
        }
    }
}
