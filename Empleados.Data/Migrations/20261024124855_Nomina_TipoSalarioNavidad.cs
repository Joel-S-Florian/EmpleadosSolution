using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Empleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class Nomina_TipoSalarioNavidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Nominas_EmpresaId_Anio_Mes_Frecuencia_Quincena",
                table: "Nominas");

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Nominas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Regular"); // las nóminas existentes son regulares

            migrationBuilder.CreateIndex(
                name: "IX_Nominas_EmpresaId_Tipo_Anio_Mes_Frecuencia_Quincena",
                table: "Nominas",
                columns: new[] { "EmpresaId", "Tipo", "Anio", "Mes", "Frecuencia", "Quincena" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Nominas_EmpresaId_Tipo_Anio_Mes_Frecuencia_Quincena",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Nominas");

            migrationBuilder.CreateIndex(
                name: "IX_Nominas_EmpresaId_Anio_Mes_Frecuencia_Quincena",
                table: "Nominas",
                columns: new[] { "EmpresaId", "Anio", "Mes", "Frecuencia", "Quincena" },
                unique: true);
        }
    }
}
