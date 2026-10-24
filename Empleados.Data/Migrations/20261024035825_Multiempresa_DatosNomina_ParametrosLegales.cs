using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Empleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class Multiempresa_DatosNomina_ParametrosLegales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Empleados_CorreoElectronico",
                table: "Empleados");

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Puestos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Afp",
                table: "Empleados",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ars",
                table: "Empleados",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BancoId",
                table: "Empleados",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DependientesSfs",
                table: "Empleados",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Empleados",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinContrato",
                table: "Empleados",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrecuenciaPago",
                table: "Empleados",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Quincenal");

            migrationBuilder.AddColumn<string>(
                name: "Nss",
                table: "Empleados",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroCuenta",
                table: "Empleados",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento",
                table: "Empleados",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sexo",
                table: "Empleados",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoContrato",
                table: "Empleados",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Indefinido");

            migrationBuilder.AddColumn<string>(
                name: "TipoCuenta",
                table: "Empleados",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "Empleados",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Cedula");

            migrationBuilder.AddColumn<string>(
                name: "TipoSalario",
                table: "Empleados",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "FijoMensual");

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Departamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Bancos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bancos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rnc = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NombreComercial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Tamano = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TasaRiesgoSrl = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FrecuenciaPagoPredeterminada = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MesCierreFiscal = table.Column<int>(type: "int", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CorreoElectronico = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feriados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feriados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosEmpleados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ValorAnterior = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ValorNuevo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UsuarioNombre = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosEmpleados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosEmpleados_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParametrosLegales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Clave = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VigenteDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fuente = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosLegales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalariosMinimos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VigenteDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tamano = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalariosMinimos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TramosIsr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VigenteDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Desde = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Hasta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Tasa = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CuotaFija = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TramosIsr", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosEmpresas",
                columns: table => new
                {
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosEmpresas", x => new { x.UsuarioId, x.EmpresaId });
                    table.ForeignKey(
                        name: "FK_UsuariosEmpresas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosEmpresas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Bancos",
                columns: new[] { "Id", "Codigo", "Nombre" },
                values: new object[,]
                {
                    { 1, "BHD", "Banco BHD" },
                    { 2, "BANRESERVAS", "Banco de Reservas (Banreservas)" },
                    { 3, "POPULAR", "Banco Popular Dominicano" },
                    { 4, "SCOTIABANK", "Scotiabank" },
                    { 5, "BANESCO", "Banesco" },
                    { 6, "SANTACRUZ", "Banco Santa Cruz" },
                    { 7, "PROMERICA", "Banco Promerica" },
                    { 8, "CARIBE", "Banco Caribe" },
                    { 9, "BDI", "Banco BDI" },
                    { 10, "VIMENCA", "Banco Vimenca" },
                    { 11, "LOPEZDEHARO", "Banco López de Haro" },
                    { 12, "LAFISE", "Banco Lafise" },
                    { 13, "ADEMI", "Banco Ademi" },
                    { 14, "CITI", "Citibank" },
                    { 15, "APAP", "Asociación Popular de Ahorros y Préstamos" },
                    { 16, "CIBAO", "Asociación Cibao de Ahorros y Préstamos" }
                });

            migrationBuilder.InsertData(
                table: "Feriados",
                columns: new[] { "Id", "Fecha", "Nombre" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Año Nuevo" },
                    { 2, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de los Santos Reyes (trasladado)" },
                    { 3, new DateTime(2026, 1, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de la Altagracia" },
                    { 4, new DateTime(2026, 1, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de Duarte" },
                    { 5, new DateTime(2026, 2, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de la Independencia" },
                    { 6, new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Viernes Santo" },
                    { 7, new DateTime(2026, 5, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día del Trabajo (trasladado)" },
                    { 8, new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Corpus Christi" },
                    { 9, new DateTime(2026, 8, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de la Restauración" },
                    { 10, new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de las Mercedes" },
                    { 11, new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de la Constitución (trasladado)" },
                    { 12, new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Navidad" }
                });

            migrationBuilder.InsertData(
                table: "ParametrosLegales",
                columns: new[] { "Id", "Clave", "Fuente", "Valor", "VigenteDesde" },
                values: new object[,]
                {
                    { 1, "TSS.AFP.Empleado", "TSS 2026 (Ley 87-01, Res. 01-2025)", 2.87m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "TSS.AFP.Empleador", "TSS 2026 (Ley 87-01, Res. 01-2025)", 7.10m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, "TSS.SFS.Empleado", "TSS 2026 (Ley 87-01, Res. 01-2025)", 3.04m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, "TSS.SFS.Empleador", "TSS 2026 (Ley 87-01, Res. 01-2025)", 7.09m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, "TSS.SalarioMinimoCotizable", "TSS 2026 (Ley 87-01, Res. 01-2025)", 23223m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, "TSS.TopeAFP.SalariosMinimos", "TSS 2026 (Ley 87-01, Res. 01-2025)", 20m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 7, "TSS.TopeSFS.SalariosMinimos", "TSS 2026 (Ley 87-01, Res. 01-2025)", 10m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 8, "TSS.TopeSRL.SalariosMinimos", "TSS 2026 (Ley 87-01, Res. 01-2025)", 4m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 9, "INFOTEP.Empleador", "Ley 116-80 (INFOTEP)", 1.00m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 10, "INFOTEP.EmpleadoBonificacion", "Ley 116-80 (INFOTEP)", 0.50m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 11, "Laboral.DivisorSalarioDiario", "Código de Trabajo, Ley 16-92", 23.83m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 12, "Laboral.HorasJornadaDiaria", "Código de Trabajo, Ley 16-92", 8m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 13, "Laboral.HorasJornadaSemanal", "Código de Trabajo, Ley 16-92", 44m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 14, "Laboral.UmbralHorasExtraExceso", "Código de Trabajo, Ley 16-92, art. 203", 68m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 15, "Laboral.RecargoHoraExtra", "Código de Trabajo, Ley 16-92, art. 203", 35m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 16, "Laboral.RecargoHoraExtraExceso", "Código de Trabajo, Ley 16-92, art. 203", 100m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 17, "Laboral.RecargoNocturno", "Código de Trabajo, Ley 16-92, art. 204", 15m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 18, "Laboral.RecargoFeriado", "Código de Trabajo, Ley 16-92", 100m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 19, "Laboral.VacacionesDias.Hasta5Anios", "Código de Trabajo, Ley 16-92, art. 177", 14m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 20, "Laboral.VacacionesDias.Mas5Anios", "Código de Trabajo, Ley 16-92, art. 177", 18m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 21, "Laboral.LicenciaPaternidadDias", "Código de Trabajo, Ley 16-92, art. 54", 2m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 22, "Laboral.LicenciaMaternidadSemanas", "Res. CNSS 418-02 / Convenio 183 OIT", 14m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "SalariosMinimos",
                columns: new[] { "Id", "Monto", "Tamano", "VigenteDesde" },
                values: new object[,]
                {
                    { 1, 29988.00m, "Grande", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 27489.60m, "Mediana", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 18421.20m, "Pequena", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, 16993.20m, "Micro", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "TramosIsr",
                columns: new[] { "Id", "CuotaFija", "Desde", "Hasta", "Tasa", "VigenteDesde" },
                values: new object[,]
                {
                    { 1, 0m, 0m, 416220m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 0m, 416220m, 624329m, 15m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 31216m, 624329m, 867123m, 20m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, 79776m, 867123m, null, 25m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Puestos_EmpresaId",
                table: "Puestos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_BancoId",
                table: "Empleados",
                column: "BancoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_EmpresaId_CorreoElectronico",
                table: "Empleados",
                columns: new[] { "EmpresaId", "CorreoElectronico" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_EmpresaId_NumeroDocumento",
                table: "Empleados",
                columns: new[] { "EmpresaId", "NumeroDocumento" },
                unique: true,
                filter: "[NumeroDocumento] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departamentos_EmpresaId",
                table: "Departamentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Bancos_Codigo",
                table: "Bancos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Rnc",
                table: "Empresas",
                column: "Rnc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_Fecha",
                table: "Feriados",
                column: "Fecha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosEmpleados_EmpleadoId_Fecha",
                table: "MovimientosEmpleados",
                columns: new[] { "EmpleadoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosLegales_Clave_VigenteDesde",
                table: "ParametrosLegales",
                columns: new[] { "Clave", "VigenteDesde" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalariosMinimos_Tamano_VigenteDesde",
                table: "SalariosMinimos",
                columns: new[] { "Tamano", "VigenteDesde" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosEmpresas_EmpresaId",
                table: "UsuariosEmpresas",
                column: "EmpresaId");

            // Datos existentes (antes de ser multiempresa): todo pasa a una empresa inicial y
            // los usuarios actuales reciben acceso a ella. Los datos reales de la empresa se completan en la pantalla de Empresas.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Departamentos) OR EXISTS (SELECT 1 FROM AspNetUsers)
                BEGIN
                    INSERT INTO Empresas (Rnc, RazonSocial, Tamano, TasaRiesgoSrl, FrecuenciaPagoPredeterminada, MesCierreFiscal, Activa)
                    VALUES ('000000000', 'Empresa Demo SRL', 'Grande', 1.20, 'Quincenal', 12, 1);
                    DECLARE @empresaId int = SCOPE_IDENTITY();
                    UPDATE Departamentos SET EmpresaId = @empresaId;
                    UPDATE Puestos SET EmpresaId = @empresaId;
                    UPDATE Empleados SET EmpresaId = @empresaId;
                    INSERT INTO UsuariosEmpresas (UsuarioId, EmpresaId) SELECT Id, @empresaId FROM AspNetUsers;
                END
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Departamentos_Empresas_EmpresaId",
                table: "Departamentos",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Empleados_Bancos_BancoId",
                table: "Empleados",
                column: "BancoId",
                principalTable: "Bancos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Empleados_Empresas_EmpresaId",
                table: "Empleados",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Puestos_Empresas_EmpresaId",
                table: "Puestos",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departamentos_Empresas_EmpresaId",
                table: "Departamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_Bancos_BancoId",
                table: "Empleados");

            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_Empresas_EmpresaId",
                table: "Empleados");

            migrationBuilder.DropForeignKey(
                name: "FK_Puestos_Empresas_EmpresaId",
                table: "Puestos");

            migrationBuilder.DropTable(
                name: "Bancos");

            migrationBuilder.DropTable(
                name: "Feriados");

            migrationBuilder.DropTable(
                name: "MovimientosEmpleados");

            migrationBuilder.DropTable(
                name: "ParametrosLegales");

            migrationBuilder.DropTable(
                name: "SalariosMinimos");

            migrationBuilder.DropTable(
                name: "TramosIsr");

            migrationBuilder.DropTable(
                name: "UsuariosEmpresas");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropIndex(
                name: "IX_Puestos_EmpresaId",
                table: "Puestos");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_BancoId",
                table: "Empleados");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_EmpresaId_CorreoElectronico",
                table: "Empleados");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_EmpresaId_NumeroDocumento",
                table: "Empleados");

            migrationBuilder.DropIndex(
                name: "IX_Departamentos_EmpresaId",
                table: "Departamentos");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Puestos");

            migrationBuilder.DropColumn(
                name: "Afp",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "Ars",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "BancoId",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "DependientesSfs",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "FechaFinContrato",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "FrecuenciaPago",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "Nss",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "NumeroCuenta",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "NumeroDocumento",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "Sexo",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "TipoContrato",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "TipoCuenta",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "TipoSalario",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Departamentos");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_CorreoElectronico",
                table: "Empleados",
                column: "CorreoElectronico",
                unique: true);
        }
    }
}
